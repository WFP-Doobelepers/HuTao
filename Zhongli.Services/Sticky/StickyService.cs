using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Zhongli.Data;
using Zhongli.Data.Models.Discord.Message;
using Zhongli.Data.Models.Discord.Message.Components;
using Zhongli.Services.Utilities;
using static Zhongli.Services.Utilities.EmbedBuilderExtensions.EmbedBuilderOptions;

namespace Zhongli.Services.Sticky;

public class StickyService
{
    private readonly IMemoryCache _cache;
    private readonly ZhongliContext _db;

    public StickyService(IMemoryCache cache, ZhongliContext db)
    {
        _cache = cache;
        _db    = db;
    }

    public async Task AddAsync(StickyMessage sticky, IGuild guild)
    {
        var existing = await _db.Set<StickyMessage>()
            .FirstOrDefaultAsync(s => s.ChannelId == sticky.ChannelId);

        if (existing is not null)
            _db.Remove(existing);

        var messages = await GetStickyMessages(guild);

        messages.Add(sticky);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(StickyMessage sticky)
    {
        var template = sticky.Template;

        _cache.Remove(template.Id);

        foreach (var component in template.Components.SelectMany(c => c.Components))
        {
            _db.Remove(component);
            if (component is SelectMenu menu)
                _db.RemoveRange(menu.Options);
        }

        foreach (var embed in template.Embeds)
        {
            _db.RemoveRange(embed.Fields);
            _db.TryRemove(embed.Author);
            _db.TryRemove(embed.Footer);
            _db.TryRemove(embed.Image);
            _db.TryRemove(embed.Thumbnail);
            _db.TryRemove(embed.Video);
        }

        _db.RemoveRange(template.Attachments);
        _db.RemoveRange(template.Components);
        _db.RemoveRange(template.Embeds);

        _db.Remove(template);
        _db.Remove(sticky);

        await _db.SaveChangesAsync();
    }

    public async Task SendStickyMessage(StickyMessage sticky, ITextChannel channel)
    {
        var template = sticky.Template;

        if (!ShouldResendSticky(sticky, out var details)) return;

        while (details.Messages.TryDequeue(out var message))
        {
            _ = message?.DeleteAsync();
        }

        var options = template.ReplaceTimestamps ? ReplaceTimestamps : None;
        var embeds = template.Embeds.Select(e => e.ToBuilder(options));
        var components = template.Components.ToBuilder();

        if (details.Token.IsCancellationRequested) return;
        details.Messages.Enqueue(await channel.SendMessageAsync(
            template.Content,
            allowedMentions: template.AllowMentions
                ? AllowedMentions.All
                : AllowedMentions.None,
            embeds: embeds.Select(e => e.Build()).ToArray(),
            components: components?.Build()));
    }

    public async Task<ICollection<StickyMessage>> GetStickyMessages(IGuild guild)
    {
        var entity = await _db.Guilds.TrackGuildAsync(guild);
        return entity.StickyMessages;
    }

    private bool ShouldResendSticky(StickyMessage sticky, out StickyMessageDetails details)
    {
        details = _cache.GetOrCreate(sticky.Id, e =>
        {
            e.SlidingExpiration = TimeSpan.FromDays(1);
            return new StickyMessageDetails();
        });

        lock (details)
        {
            details.MessageCount++;
            if (details.MessageCount < sticky.CountDelay)
                return false;

            if (details.LastSent > DateTimeOffset.UtcNow - sticky.TimeDelay)
                return false;

            details.Token.Cancel();

            details.Token        = new CancellationTokenSource();
            details.LastSent     = DateTimeOffset.UtcNow;
            details.MessageCount = 0;

            return true;
        }
    }
}