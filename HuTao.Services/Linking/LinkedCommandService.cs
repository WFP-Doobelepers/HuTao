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
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Messages;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using CommandContext = HuTao.Data.Models.Discord.CommandContext;

namespace HuTao.Services.Linking;

public class LinkedCommandService(
    CommandErrorHandler error,
    CommandService commands,
    DiscordSocketClient client,
    IMemoryCache cache,
    HuTaoContext db)
    : INotificationHandler<ReadyNotification>
{
    public async Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
    {
        foreach (var guild in db.Guilds.ToList())
        {
            await AddCommandsAsync(guild);
        }
    }

    public async Task DeleteAsync(LinkedCommand command)
    {
        db.Remove(command);
        db.TryRemove(command.Message);
        db.TryRemove(command.Authorization);
        db.RemoveRange(command.Roles);

        await db.SaveChangesAsync();
    }

    public async Task RefreshCommandsAsync(IGuild guild)
    {
        var guildEntity = await db.Guilds.TrackGuildAsync(guild);
        await AddCommandsAsync(guildEntity);
    }

    private DateTimeOffset? GetLastRun(LinkedCommand command)
    {
        if (command.Cooldown is null) return null;

        if (cache.TryGetValue<DateTimeOffset>(command.Id, out var lastRun))
            return lastRun;

        cache.Set(command.Id, DateTimeOffset.UtcNow, command.Cooldown.Value);
        return null;
    }

    private static async IAsyncEnumerable<EmbedBuilder> AddRolesAsync(
        IEnumerable<IGuildUser> users, ICollection<RoleTemplate> templates)
    {
        var task = users.Select(u => u.AddRolesAsync(templates));
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

        if (client.GetGuild(guild.Id) is not { } socketGuild)
            return;

        await commands.CreateModuleAsync(string.Empty, m =>
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

        var guild = await db.Guilds.TrackGuildAsync(context.Guild);
        var command = guild.LinkedCommands.FirstOrDefault(c => c.Id == id);
        if (command is null)
            return;

        var authorized = AuthorizationService.IsAuthorized(context, command.Authorization, true);
        if (!authorized)
        {
            await context.ReplyAsync("You are not allowed to use this command.");
            return;
        }

        var lastRun = GetLastRun(command);
        if (lastRun is not null)
        {
            var message = $"Please wait {(lastRun + command.Cooldown).Humanize()} before using this command.";
            await error.AssociateError(context, message);
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
            await db.UpdateAsync(template, context.Guild);

        var embeds = template?.GetEmbedBuilders().ToList() ?? [];
        var roleTemplates = command.Roles.ToArray();

        var flags = template?.SuppressEmbeds ?? false ? MessageFlags.SuppressEmbeds : MessageFlags.None;
        var allowedMentions = template?.AllowMentions ?? false ? AllowedMentions.All : AllowedMentions.None;
        const uint defaultAccentColor = 0x9B59FF;
        if (command.UserOptions.HasFlag(UserTargetOptions.DmUser))
        {
            foreach (var user in users)
            {
                var roles = await LinkingService.ApplyRoleTemplatesAsync(user, roleTemplates).ToListAsync();
                var dm = await user.CreateDMChannelAsync();

                try
                {
                    await dm.SendMessageAsync($"This message was sent from {context.Guild.Name}.");

                    var builtEmbeds = embeds.Concat(roles).Select(e => e.Build()).ToList();
                    var builder = new ComponentBuilderV2();

                    if (!string.IsNullOrWhiteSpace(template?.Content))
                    {
                        builder.WithContainer(new ContainerBuilder()
                            .WithTextDisplay(template.Content.Truncate(4000))
                            .WithAccentColor(defaultAccentColor));
                    }

                    foreach (var embed in builtEmbeds)
                        builder.WithContainer(embed.ToComponentsV2Container());

                    if (template is not null)
                    {
                        foreach (var row in template.Components.ToActionRowBuilders())
                            builder.WithActionRow(row);
                    }

                    if (string.IsNullOrWhiteSpace(template?.Content) && builtEmbeds.Count == 0)
                    {
                        builder.WithContainer(new ContainerBuilder()
                            .WithTextDisplay("-# (empty template)")
                            .WithAccentColor(defaultAccentColor));
                    }

                    await dm.SendMessageAsync(
                        components: builder.Build(),
                        allowedMentions: allowedMentions,
                        flags: flags);
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

        var built = embeds.Select(e => e.Build()).ToList();
        var componentBuilder = new ComponentBuilderV2();

        if (!string.IsNullOrWhiteSpace(template?.Content))
        {
            componentBuilder.WithContainer(new ContainerBuilder()
                .WithTextDisplay(template.Content.Truncate(4000))
                .WithAccentColor(defaultAccentColor));
        }

        foreach (var embed in built)
            componentBuilder.WithContainer(embed.ToComponentsV2Container());

        if (template is not null)
        {
            foreach (var row in template.Components.ToActionRowBuilders())
                componentBuilder.WithActionRow(row);
        }

        if (string.IsNullOrWhiteSpace(template?.Content) && built.Count == 0)
        {
            componentBuilder.WithContainer(new ContainerBuilder()
                .WithTextDisplay("-# (empty template)")
                .WithAccentColor(defaultAccentColor));
        }

        await context.ReplyAsync(
            components: componentBuilder.Build(),
            allowedMentions: allowedMentions,
            flags: flags,
            ephemeral: command.Ephemeral);
    }

    private async Task RemoveModuleAsync(GuildEntity guild)
    {
        var module = commands.Modules.FirstOrDefault(m => m.Preconditions
            .Any(a => a is GuildCommandAttribute g && g.GuildId == guild.Id));

        if (module is not null)
            await commands.RemoveModuleAsync(module);
    }

    private class GuildCommandAttribute(ulong guildId) : PreconditionAttribute
    {
        public ulong GuildId { get; } = guildId;

        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandInfo command,
            IServiceProvider services) => context.Guild.Id == GuildId
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("This command is not available in this guild."));
    }
}