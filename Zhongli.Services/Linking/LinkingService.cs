using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Discord.Message.Components;
using Zhongli.Data.Models.Discord.Message.Linking;
using Zhongli.Services.Utilities;

namespace Zhongli.Services.Linking;

public class LinkingService
{
    private readonly ZhongliContext _db;

    public LinkingService(ZhongliContext db) { _db = db; }

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
        await context.Interaction.DeferAsync();

        var button = await _db.Set<LinkedButton>().FindAsync(id);
        if (button is null || button.Guild.Id != context.Guild.Id) return;

        var template = button.Message;
        if (template?.IsLive ?? false)
            await _db.UpdateAsync(template, context.Guild);

        var roles = await ApplyRoleTemplatesAsync(context, button.Roles.ToList());
        var embeds = new List<EmbedBuilder>()
            .Concat(template?.GetEmbedBuilders() ?? Enumerable.Empty<EmbedBuilder>())
            .Concat(roles);

        await context.Interaction.FollowupAsync(template?.Content,
            embeds.Select(e => e.Build()).ToArray(),
            components: template?.Components.ToBuilder().Build(),
            ephemeral: button.Ephemeral);
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

        var component = rows.AddComponent(linked.Button);
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

        template.Components.AddComponent(linked.Button).ToBuilder().Build();

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

    private LinkedButton GetButton(GuildEntity guild, LinkedButton button)
    {
        guild.LinkedButtons.Add(button);
        _db.UpdateRange(guild.LinkedButtons);

        if (button.Button.Style is not ButtonStyle.Link)
            button.Button.CustomId = $"linked:{button.Id}";

        return button;
    }

    private static async Task<IEnumerable<EmbedBuilder>> ApplyRoleTemplatesAsync(
        IInteractionContext context, IReadOnlyCollection<RoleTemplate> templates)
    {
        if (context.User is not IGuildUser user)
            return Enumerable.Empty<EmbedBuilder>();

        var added = new List<RoleTemplate>();
        var removed = new List<RoleTemplate>();

        foreach (var add in templates.Where(t => t.Behavior == RoleBehavior.Add))
        {
            try
            {
                await user.AddRoleAsync(add.RoleId);
                added.Add(add);
            }
            catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
            {
                // Ignored
            }
        }

        foreach (var remove in templates.Where(t => t.Behavior == RoleBehavior.Remove))
        {
            try
            {
                await user.RemoveRoleAsync(remove.RoleId);
                removed.Add(remove);
            }
            catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
            {
                // Ignored
            }
        }

        foreach (var toggle in templates.Where(t => t.Behavior == RoleBehavior.Toggle))
        {
            try
            {
                if (user.HasRole(toggle.RoleId))
                {
                    await user.RemoveRoleAsync(toggle.RoleId);
                    removed.Add(toggle);
                }
                else
                {
                    await user.AddRoleAsync(toggle.RoleId);
                    added.Add(toggle);
                }
            }
            catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
            {
                // Ignored
            }
        }

        if (added.Count is 0 && removed.Count is 0)
            return Enumerable.Empty<EmbedBuilder>();

        return new[]
        {
            new EmbedBuilder()
                .WithTitle("Added roles")
                .WithColor(Color.Green)
                .WithDescription(added.Humanize(r => r.MentionRole())),

            new EmbedBuilder()
                .WithTitle("Removed roles")
                .WithColor(Color.Red)
                .WithDescription(removed.Humanize(r => r.MentionRole()))
        };
    }
}