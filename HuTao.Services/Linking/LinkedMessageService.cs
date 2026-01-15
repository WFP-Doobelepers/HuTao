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

public class LinkingService(IMemoryCache cache, HuTaoContext db)
{
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
        var rows = db.Set<ActionRow>();
        var row = await rows.FirstOrDefaultAsync(r => r.Components.All(c => c == button.Button));

        db.TryRemove(button);
        db.TryRemove(row);

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(MessageTemplate template)
    {
        db.TryRemove(template);
        await db.SaveChangesAsync();
    }

    public async Task SendMessageAsync(IInteractionContext context, Guid id)
    {
        if (GetLastRun(context, id) is not null) return;

        var button = await db.Set<LinkedButton>().FindAsync(id);
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

        var guild = await db.Guilds.TrackGuildAsync(channel.Guild);

        var actionRows = message.Components.OfType<ActionRowComponent>();
        var rows = actionRows.Select(r => new ActionRow(r)).ToList();

        var builder = GetButtonBuilder(options);
        var linked = GetButton(guild, new LinkedButton(builder, options));

        rows.AddComponent(linked.Button, options.Row);

        var updated = BuildComponentsV2(message.Components, rows);

        await message.ModifyAsync(m => m.Components = updated);

        await db.SaveChangesAsync();
        return linked;
    }

    public async Task<LinkedButton?> LinkTemplateAsync(
        Context context, MessageTemplate template, ILinkedButtonOptions options)
    {
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        var builder = GetButtonBuilder(options);
        var linked = GetButton(guild, new LinkedButton(builder, options));

        template.Components.AddComponent(linked.Button, options.Row);

        await db.SaveChangesAsync();
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
        if (cache.TryGetValue<DateTimeOffset>(key, out var lastRun))
            return lastRun;

        cache.Set(key, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(15));
        return null;
    }

    private LinkedButton GetButton(GuildEntity guild, LinkedButton button)
    {
        guild.LinkedButtons.Add(button);
        db.UpdateRange(guild.LinkedButtons);

        if (button.Button.Style is not ButtonStyle.Link)
            button.Button.CustomId = $"linked:{button.Id}";

        return button;
    }

    private static MessageComponent BuildComponentsV2(
        IReadOnlyCollection<IMessageComponent> existing,
        IReadOnlyCollection<ActionRow> rows)
    {
        var builder = new ComponentBuilderV2();

        foreach (var component in existing)
        {
            if (component is ContainerComponent container)
                builder.WithContainer(new ContainerBuilder(container));
        }

        foreach (var actionRow in rows.ToActionRowBuilders())
            builder.WithActionRow(actionRow);

        return builder.Build();
    }

    private async Task SendMessageAsync(
        Context context, MessageTemplate? template,
        ICollection<RoleTemplate> templates, bool isEphemeral, bool dmUser)
    {
        if (!dmUser) await context.DeferAsync();
        if (template?.IsLive ?? false)
            await db.UpdateAsync(template, context.Guild);

        var flags = template?.SuppressEmbeds ?? false ? MessageFlags.SuppressEmbeds : MessageFlags.None;
        var allowedMentions = template?.AllowMentions ?? false ? AllowedMentions.All : AllowedMentions.None;
        var roles = await ApplyRoleTemplatesAsync(context.User, templates).ToListAsync();
        var embeds = new List<EmbedBuilder>()
            .Concat(template?.GetEmbedBuilders() ?? [])
            .Concat(roles);

        var builtEmbeds = embeds.Select(e => e.Build()).ToList();

        const uint defaultAccentColor = 0x9B59FF;
        var builder = new ComponentBuilderV2();

        if (!string.IsNullOrWhiteSpace(template?.Content))
        {
            builder.WithContainer(new ContainerBuilder()
                .WithTextDisplay(template.Content)
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

        var components = builder.Build();

        if (dmUser)
        {
            var dm = await context.User.CreateDMChannelAsync();
            await dm.SendMessageAsync(
                components: components,
                allowedMentions: allowedMentions,
                flags: flags);
        }
        else
        {
            await context.ReplyAsync(
                flags: flags,
                components: components,
                allowedMentions: allowedMentions,
                ephemeral: isEphemeral);
        }
    }
}