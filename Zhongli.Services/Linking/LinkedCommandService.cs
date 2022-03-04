using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Humanizer;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message.Linking;
using Zhongli.Services.CommandHelp;
using Zhongli.Services.Core;
using Zhongli.Services.Core.Listeners;
using Zhongli.Services.Core.Messages;
using Zhongli.Services.Utilities;
using CommandContext = Zhongli.Data.Models.Discord.CommandContext;

namespace Zhongli.Services.Linking;

public class LinkedCommandService : INotificationHandler<ReadyNotification>
{
    private readonly AuthorizationService _auth;
    private readonly CommandErrorHandler _error;
    private readonly CommandService _commands;
    private readonly DiscordSocketClient _client;
    private readonly IMemoryCache _cache;
    private readonly ZhongliContext _db;

    public LinkedCommandService(
        AuthorizationService auth, CommandErrorHandler error, CommandService commands,
        DiscordSocketClient client, IMemoryCache cache, ZhongliContext db)
    {
        _auth     = auth;
        _error    = error;
        _commands = commands;
        _client   = client;
        _cache    = cache;
        _db       = db;
    }

    public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        foreach (var guild in _db.Guilds.ToList())
        {
            await AddCommandsAsync(guild);
        }
    }

    public async Task DeleteAsync(LinkedCommand command)
    {
        _db.Remove(command);
        _db.TryRemove(command.Message);
        _db.RemoveRange(command.Inclusions);
        _db.RemoveRange(command.Roles);

        await _db.SaveChangesAsync();
    }

    public async Task RefreshCommandsAsync(IGuild guild)
    {
        var guildEntity = await _db.Guilds.TrackGuildAsync(guild);
        await AddCommandsAsync(guildEntity);
    }

    private DateTimeOffset? GetLastRun(LinkedCommand command)
    {
        if (command.Cooldown is null) return null;

        if (_cache.TryGetValue<DateTimeOffset>(command.Id, out var lastRun))
            return lastRun;

        _cache.Set(command.Id, DateTimeOffset.UtcNow, command.Cooldown.Value);
        return null;
    }

    private static async IAsyncEnumerable<EmbedBuilder> AddRolesAsync(
        IEnumerable<IGuildUser> users, IReadOnlyCollection<RoleTemplate> templates)
    {
        var task = users.Select(u => LinkingService.AddRolesAsync(u, templates));
        var roles = await Task.WhenAll(task);

        var added = roles.SelectMany(r => r.Added).ToList();
        var removed = roles.SelectMany(r => r.Removed).ToList();

        if (added.Any())
        {
            yield return new EmbedBuilder()
                .WithTitle("Added roles")
                .WithColor(Color.Green)
                .WithDescription(added.Humanize(r => $"{r.Template.MentionRole()} to {r.User.Mention}"));
        }

        if (removed.Any())
        {
            yield return new EmbedBuilder()
                .WithTitle("Removed roles")
                .WithColor(Color.Red)
                .WithDescription(removed.Humanize(r => $"{r.Template.MentionRole()} from {r.User.Mention}"));
        }
    }

    private async Task AddCommandsAsync(GuildEntity guild)
    {
        await RemoveModuleAsync(guild);

        if (guild.LinkedCommands.Count is 0)
            return;

        if (_client.GetGuild(guild.Id) is not { } socketGuild)
            return;

        await _commands.CreateModuleAsync(string.Empty, m =>
        {
            m.WithName($"Custom Commands for {socketGuild.Name}");
            m.AddAttributes(new HiddenFromHelpAttribute());
            m.AddPrecondition(new GuildCommandAttribute(guild.Id));
            foreach (var command in guild.LinkedCommands.ToList())
            {
                m.AddCommand(command.Name, async (context, args, services, _) =>
                {
                    if (context.Guild.Id != guild.Id) return;
                    var service = services.GetRequiredService<LinkedCommandService>();
                    await service.ExecuteCommandAsync(command.Id, new CommandContext(context), args);
                }, c =>
                {
                    c.Summary = command.Description;

                    if (command.UserOptions.HasFlag(UserTargetOptions.ApplyMentions))
                    {
                        c.AddParameter<IGuildUser>("users", p =>
                        {
                            p.IsOptional = command.UserOptions.HasFlag(UserTargetOptions.ApplySelf);
                            p.Summary    = "The users to apply the command to.";
                            p.IsMultiple = true;
                        });
                    }
                });
            }
        });
    }

    private async Task ExecuteCommandAsync(Guid id, Context context, IEnumerable<object> args)
    {
        await context.DeferAsync();

        var guild = await _db.Guilds.TrackGuildAsync(context.Guild);
        var command = guild.LinkedCommands.FirstOrDefault(c => c.Id == id);
        if (command is null)
            return;

        var included = command.Inclusions.All(c => c.Judge(context));
        var authorized = await _auth.IsAuthorizedAsync(context, command.Scope | AuthorizationScope.All);
        if (!included && !authorized)
        {
            await context.ReplyAsync("You are not allowed to use this command.");
            return;
        }

        var lastRun = GetLastRun(command);
        if (lastRun is not null)
        {
            var message = $"Please wait {(lastRun + command.Cooldown).Humanize()} before using this command.";
            await _error.AssociateError(context, message);
            return;
        }

        if (command.Silent && context is CommandContext commandContext)
            _ = commandContext.Message.DeleteAsync();

        var users = new List<IGuildUser>();
        if (command.UserOptions.HasFlag(UserTargetOptions.ApplySelf) && context.User is IGuildUser guildUser)
            users.Add(guildUser);

        if (command.UserOptions.HasFlag(UserTargetOptions.ApplyMentions)
            && args.FirstOrDefault() is IGuildUser[] guildUsers)
            users.AddRange(guildUsers);

        var template = command.Message;
        if (template?.IsLive ?? false)
            await _db.UpdateAsync(template, context.Guild);

        var embeds = template?.GetEmbedBuilders().ToList() ?? new List<EmbedBuilder>();
        var roleTemplates = command.Roles.ToArray();

        if (command.UserOptions.HasFlag(UserTargetOptions.DmUser))
        {
            foreach (var user in users)
            {
                var roles = await LinkingService.ApplyRoleTemplatesAsync(user, roleTemplates).ToListAsync();
                var dm = await user.CreateDMChannelAsync();

                try
                {
                    await dm.SendMessageAsync($"This message was sent from {context.Guild.Name}.");
                    await dm.SendMessageAsync(template?.Content,
                        components: template?.Components.ToBuilder().Build(),
                        embeds: embeds.Concat(roles).Select(e => e.Build()).ToArray());
                }
                catch (HttpException e) when (e.DiscordCode is DiscordErrorCode.CannotSendMessageToUser)
                {
                    // Ignored
                }
            }
        }
        else
        {
            var roles = AddRolesAsync(users, roleTemplates);
            embeds.AddRange(await roles.ToListAsync());
        }

        await context.ReplyAsync(template?.Content,
            components: template?.Components.ToBuilder().Build(),
            embeds: embeds.Select(e => e.Build()).ToArray(),
            ephemeral: command.Ephemeral);
    }

    private async Task RemoveModuleAsync(GuildEntity guild)
    {
        var module = _commands.Modules.FirstOrDefault(m => m.Preconditions
            .Any(a => a is GuildCommandAttribute g && g.GuildId == guild.Id));

        if (module is not null)
            await _commands.RemoveModuleAsync(module);
    }

    private class GuildCommandAttribute : PreconditionAttribute
    {
        public GuildCommandAttribute(ulong guildId) { GuildId = guildId; }

        public ulong GuildId { get; }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services) => context.Guild.Id == GuildId
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("This command is not available in this guild."));
    }
}