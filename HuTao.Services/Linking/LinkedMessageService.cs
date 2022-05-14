using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message.Components;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.Linking;

public class LinkingService
{
    private readonly HuTaoContext _db;
    private readonly IMemoryCache _cache;

    public LinkingService(IMemoryCache cache, HuTaoContext db)
    {
        _cache = cache;
        _db    = db;
    }

    public static async IAsyncEnumerable<EmbedBuilder> ApplyRoleTemplatesAsync(
        IUser user, ICollection<RoleTemplate> templates)
    {
        if (user is not IGuildUser guildUser)
            yield break;

        var (added, removed) = await guildUser.AddRolesAsync(templates);

        if (added.Any())
        {
            yield return new EmbedBuilder()
                .WithTitle("Added roles")
                .WithColor(Color.Green)
                .WithDescription(added.Humanize(DisplayFormatter));
        }

        if (removed.Any())
        {
            yield return new EmbedBuilder()
                .WithTitle("Removed roles")
                .WithColor(Color.Red)
                .WithDescription(removed.Humanize(DisplayFormatter));
        }

        static string DisplayFormatter(RoleMetadata r) => $"{r.Template.MentionRole()} ({r.Role.Name})";
    }

    public async Task DeleteAsync(LinkedButton button)
    {
        var rows = _db.Set<ActionRow>();
        var row = await rows.FirstOrDefaultAsync(r => r.Components.All(c => c == button.Button));

        _db.TryRemove(button);
        _db.TryRemove(row);

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(MessageTemplate template)
    {
        _db.TryRemove(template);
        await _db.SaveChangesAsync();
    }

    public async Task SendMessageAsync(IInteractionContext context, Guid id)
    {
        if (GetLastRun(context, id) is not null) return;

        var button = await _db.Set<LinkedButton>().FindAsync(id);
        if (button is null || button.Guild.Id != context.Guild.Id) return;

        await SendMessageAsync(
            new InteractionContext(context),
            button.Message, button.Roles.ToArray(),
            button.Ephemeral, button.DmUser);
    }

    public async Task<LinkedButton?> LinkMessageAsync(IUserMessage message, ILinkedButtonOptions options)
    {
        if (message.Channel is not IGuildChannel channel)
            return null;

        var guild = await _db.Guilds.TrackGuildAsync(channel.Guild);

        var components = message.Components.Cast<ActionRowComponent>();
        var rows = components.Select(r => new ActionRow(r)).ToList();

        var builder = GetButtonBuilder(options);
        var linked = GetButton(guild, new LinkedButton(builder, options));

        var component = rows.AddComponent(linked.Button, options.Row);
        await message.ModifyAsync(m => m.Components = component.ToBuilder().Build());

        await _db.SaveChangesAsync();
        return linked;
    }

    public async Task<LinkedButton?> LinkTemplateAsync(
        Context context, MessageTemplate template, ILinkedButtonOptions options)
    {
        var guild = await _db.Guilds.TrackGuildAsync(context.Guild);

        var builder = GetButtonBuilder(options);
        var linked = GetButton(guild, new LinkedButton(builder, options));

        template.Components.AddComponent(linked.Button, options.Row).ToBuilder().Build();

        await _db.SaveChangesAsync();
        return linked;
    }

    private static bool IsLink(ILinkedButtonOptions options) => !string.IsNullOrWhiteSpace(options.Url);

    private static ButtonBuilder GetButtonBuilder(ILinkedButtonOptions options) => new()
    {
        CustomId   = IsLink(options) ? null : $"linked:{Guid.Empty}",
        IsDisabled = options.IsDisabled,
        Emote      = options.Emote,
        Label      = options.Label,
        Style      = IsLink(options) ? ButtonStyle.Link : options.Style,
        Url        = options.Url
    };

    private DateTimeOffset? GetLastRun(IInteractionContext context, Guid id)
    {
        var key = $"{context.User.Id}.{id}";
        if (_cache.TryGetValue<DateTimeOffset>(key, out var lastRun))
            return lastRun;

        _cache.Set(key, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(15));
        return null;
    }

    private LinkedButton GetButton(GuildEntity guild, LinkedButton button)
    {
        guild.LinkedButtons.Add(button);
        _db.UpdateRange(guild.LinkedButtons);

        if (button.Button.Style is not ButtonStyle.Link)
            button.Button.CustomId = $"linked:{button.Id}";

        return button;
    }

    private async Task SendMessageAsync(Context context, MessageTemplate? template,
        ICollection<RoleTemplate> templates, bool isEphemeral, bool dmUser)
    {
        if (!dmUser) await context.DeferAsync();
        if (template?.IsLive ?? false)
            await _db.UpdateAsync(template, context.Guild);

        var flags = template?.SuppressEmbeds ?? false ? MessageFlags.SuppressEmbeds : MessageFlags.None;
        var roles = await ApplyRoleTemplatesAsync(context.User, templates).ToListAsync();
        var embeds = new List<EmbedBuilder>()
            .Concat(template?.GetEmbedBuilders() ?? Enumerable.Empty<EmbedBuilder>())
            .Concat(roles);

        if (dmUser)
        {
            var dm = await context.User.CreateDMChannelAsync();
            await dm.SendMessageAsync(template?.Content, flags: flags,
                embeds: embeds.Select(e => e.Build()).ToArray(),
                components: template?.Components.ToBuilder().Build());
        }
        else
        {
            await context.ReplyAsync(template?.Content, flags: flags,
                components: template?.Components.ToBuilder().Build(),
                embeds: embeds.Select(e => e.Build()).ToArray(), ephemeral: isEphemeral);
        }
    }
}