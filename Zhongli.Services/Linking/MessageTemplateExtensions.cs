using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Zhongli.Data.Models.Discord.Message.Components;
using Zhongli.Data.Models.Discord.Message.Linking;
using Zhongli.Services.Utilities;
using Embed = Zhongli.Data.Models.Discord.Message.Embeds.Embed;
using EmbedBuilderExtensions = Zhongli.Services.Utilities.EmbedBuilderExtensions;

namespace Zhongli.Services.Linking;

public static class MessageTemplateExtensions
{
    public static IEnumerable<EmbedBuilder> GetEmbedBuilders(this MessageTemplate template)
        => template.Embeds.Select(e
            => e.ToBuilder(template.ReplaceTimestamps
                ? EmbedBuilderExtensions.EmbedBuilderOptions.ReplaceTimestamps
                : EmbedBuilderExtensions.EmbedBuilderOptions.None));

    public static string GetJumpUrl(this MessageTemplate template, IGuild guild)
        => $"https://discord.com/channels/{guild.Id}/{template.ChannelId}/{template.MessageId}";

    public static StringBuilder GetTemplateDetails(this MessageTemplate template, IGuild guild)
    {
        var builder = new StringBuilder()
            .AppendLine($"▌Template ID: {template.Id}")
            .AppendLine($"▌▌Content: {template.Content}")
            .AppendLine($"▌▌Live: {template.IsLive} [Jump]({template.GetJumpUrl(guild)})")
            .AppendLine($"▌▌Embeds: {template.Embeds.Count}");

        var embed = template.Embeds.FirstOrDefault();
        if (embed is not null)
        {
            builder
                .AppendLine($"▌▌Title: {embed.Title}")
                .AppendLine($"▌▌Description: {embed.Description}");
        }

        return builder;
    }

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

    internal static void TryRemove(this DbContext db, IEnumerable<ActionRow> rows)
    {
        foreach (var component in rows.SelectMany(c => c.Components))
        {
            db.Remove(component);
            if (component is SelectMenu menu)
                db.RemoveRange(menu.Options);
        }
    }

    internal static void TryRemove(this DbContext db, MessageTemplate? template)
    {
        if (template is null) return;

        db.TryRemove(template.Components);
        db.TryRemove(template.Embeds);

        db.RemoveRange(template.Attachments);
        db.RemoveRange(template.Components);
        db.RemoveRange(template.Embeds);

        db.Remove(template);
    }

    private static void TryRemove(this DbContext db, IEnumerable<Embed> embeds)
    {
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