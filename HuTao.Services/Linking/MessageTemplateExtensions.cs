using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using HuTao.Data.Models.Discord.Message.Components;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Utilities;
using Microsoft.EntityFrameworkCore;
using Embed = HuTao.Data.Models.Discord.Message.Embeds.Embed;

namespace HuTao.Services.Linking;

public static class MessageTemplateExtensions
{
    public static EmbedBuilder WithTemplateDetails(this EmbedBuilder builder,
        MessageTemplate template, IGuild guild) => builder
        .WithTitle($"Template: {template.Id}")
        .WithDescription(template.Content)
        .AddField("Live", $"{template.IsLive} **[Jump]({template.GetJumpUrl(guild)})**", true)
        .AddField("Embeds", $"{template.Embeds.Count}", true)
        .AddField("Components", template.Components.Sum(t => t.Components.Count), true)
        .WithFields(template.Embeds.Take(EmbedBuilder.MaxFieldCount - builder.Fields.Count)
            .Select((e, i) => new EmbedFieldBuilder()
                .WithName($"▌Embed {i + 1}: {e.Title}")
                .WithValue(e.Description?.Truncate(256) ?? "[No Description]")));

    public static IEnumerable<EmbedBuilder> GetEmbedBuilders(this MessageTemplate template)
        => template.Embeds.Select(e
            => e.ToBuilder(template.ReplaceTimestamps
                ? EmbedBuilderOptions.ReplaceTimestamps
                : EmbedBuilderOptions.None));

    public static Task<IUserMessage> SendMessageAsync(this MessageTemplate template, IMessageChannel channel)
        => channel.SendMessageAsync(template.Content,
            allowedMentions: template.AllowMentions ? AllowedMentions.All : AllowedMentions.None,
            embeds: template.GetEmbedBuilders().Select(e => e.Build()).ToArray(),
            components: template.Components.ToBuilder().Build());

    internal static async Task UpdateAsync(this DbContext db, MessageTemplate template, IGuild guild)
    {
        var channel = await guild.GetTextChannelAsync(template.ChannelId);
        var message = await channel.GetMessageAsync(template.MessageId);
        if (message is null) return;

        db.RemoveRange(template.Attachments);
        db.TryRemove(template.Embeds);

        template.UpdateTemplate(message);
        await db.SaveChangesAsync();
    }

    internal static void TryRemove(this DbContext db, MessageTemplate? template)
    {
        if (template is null) return;

        db.RemoveRange(template.Attachments);
        db.TryRemove(template.Components);
        db.TryRemove(template.Embeds);

        db.Remove(template);
    }

    internal static void TryRemove(this DbContext db, LinkedButton? button)
    {
        if (button is null) return;

        db.TryRemove(button.Message);
        db.RemoveRange(button.Roles);
        db.Remove(button.Button);
        db.Remove(button);
    }

    private static string GetJumpUrl(this MessageTemplate template, IGuild guild)
        => $"https://discord.com/channels/{guild.Id}/{template.ChannelId}/{template.MessageId}";

    private static void TryRemove(this DbContext db, ICollection<ActionRow> rows)
    {
        db.RemoveRange(rows);
        foreach (var row in rows)
        {
            db.RemoveRange(row.Components);
            foreach (var component in row.Components)
            {
                if (component is Button button)
                    db.TryRemove(button.Link);

                if (component is SelectMenu menu)
                    db.RemoveRange(menu.Options);
            }
        }
    }

    private static void TryRemove(this DbContext db, ICollection<Embed> embeds)
    {
        db.RemoveRange(embeds);
        foreach (var embed in embeds)
        {
            db.RemoveRange(embed.Fields);
            db.TryRemove(embed.Author);
            db.TryRemove(embed.Footer);
            db.TryRemove(embed.Image);
            db.TryRemove(embed.Thumbnail);
            db.TryRemove(embed.Video);
        }
    }
}