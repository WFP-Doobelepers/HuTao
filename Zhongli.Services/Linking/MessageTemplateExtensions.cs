using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Discord.Message.Components;
using Zhongli.Data.Models.Discord.Message.Linking;
using Zhongli.Services.Utilities;
using Embed = Zhongli.Data.Models.Discord.Message.Embeds.Embed;
using EmbedBuilderExtensions = Zhongli.Services.Utilities.EmbedBuilderExtensions;

namespace Zhongli.Services.Linking;

public static class MessageTemplateExtensions
{
    public static EmbedBuilder WithTemplateDetails(this EmbedBuilder builder,
        MessageTemplate template, IGuild guild) => builder
        .WithTitle($"Template: {template.Id}")
        .WithDescription(template.Content)
        .AddField("Live", $"{template.IsLive} **[Jump]({template.GetJumpUrl(guild)})**", true)
        .AddField("Embeds", $"{template.Embeds.Count}", true)
        .WithFields(template.Embeds.Take(EmbedBuilder.MaxFieldCount - builder.Fields.Count)
            .Select((e, i) => new EmbedFieldBuilder()
                .WithName($"▌Embed {i + 1}: {e.Title}")
                .WithValue(e.Description?.Truncate(256) ?? "[No Description]")));

    public static IEnumerable<EmbedBuilder> GetEmbedBuilders(this MessageTemplate template)
        => template.Embeds.Select(e
            => e.ToBuilder(template.ReplaceTimestamps
                ? EmbedBuilderExtensions.EmbedBuilderOptions.ReplaceTimestamps
                : EmbedBuilderExtensions.EmbedBuilderOptions.None));

    public static async Task UpdateAsync(this MessageTemplate template, IGuild guild)
    {
        var channel = await guild.GetTextChannelAsync(template.ChannelId);
        var message = await channel.GetMessageAsync(template.MessageId);
        if (message is null) return;

        template.UpdateTemplate(message);
    }

    public static Task<IUserMessage> SendMessageAsync(this MessageTemplate template, IMessageChannel channel)
        => channel.SendMessageAsync(template.Content,
            allowedMentions: template.AllowMentions ? AllowedMentions.All : AllowedMentions.None,
            embeds: template.GetEmbedBuilders().Select(e => e.Build()).ToArray(),
            components: template.Components.ToBuilder().Build());

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

    private static void TryRemove(this DbContext db, IEnumerable<Embed> embeds)
    {
        foreach (var embed in embeds)
        {
            db.Remove(embed);
            db.RemoveRange(embed.Fields);
            db.TryRemove(embed.Author);
            db.TryRemove(embed.Footer);
            db.TryRemove(embed.Image);
            db.TryRemove(embed.Thumbnail);
            db.TryRemove(embed.Video);
        }
    }
}