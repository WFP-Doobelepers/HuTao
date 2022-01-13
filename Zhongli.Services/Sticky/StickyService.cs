using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Zhongli.Data;
using Zhongli.Data.Models.Discord.Message;
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

    public async Task AddStickyMessage(StickyMessage sticky, IGuild guild)
    {
        var existing = await _db.Set<StickyMessage>()
            .FirstOrDefaultAsync(s => s.ChannelId == sticky.ChannelId);

        if (existing is not null)
            _db.Remove(existing);

        var messages = await GetStickyMessages(guild);

        messages.Add(sticky);
        await _db.SaveChangesAsync();
    }

    public async Task SendStickyMessage(StickyMessage sticky, ITextChannel channel)
    {
        var template = sticky.Template;

        var options = template.ReplaceTimestamps ? ReplaceTimestamps : None;
        var embeds = template.Embeds.Select(e => e.ToBuilder(options));
        var components = template.Components.ToBuilder();

        if (!ShouldResendSticky(sticky, out var details)) return;

        _ = details.Message?.DeleteAsync();
        details.Message = await channel.SendMessageAsync(
            template.Content,
            allowedMentions: template.AllowMentions
                ? AllowedMentions.All
                : AllowedMentions.None,
            embeds: embeds.Select(e => e.Build()).ToArray(),
            components: components?.Build());
    }

    public async Task<ICollection<StickyMessage>> GetStickyMessages(IGuild guild)
    {
        var entity = await _db.Guilds.TrackGuildAsync(guild);
        return entity.StickyMessages;
    }

    private bool ShouldResendSticky(StickyMessage sticky, out StickyMessageDetails details)
    {
        details = _cache.GetOrCreate(sticky.Id, _ => new StickyMessageDetails());

        if (details.Message is null) return true;

        details.MessageCount++;
        if (details.MessageCount < sticky.CountDelay)
            return false;

        var lastSent = DateTimeOffset.UtcNow - details.Message.Timestamp;
        if (lastSent < sticky.TimeDelay)
            return false;

        details.MessageCount = 0;
        return true;
    }
}