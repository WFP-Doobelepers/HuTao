using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using HuTao.Data;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Linking;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.Sticky;

public class StickyService
{
    private readonly HuTaoContext _db;
    private readonly IMemoryCache _cache;

    public StickyService(IMemoryCache cache, HuTaoContext db)
    {
        _cache = cache;
        _db    = db;
    }

    public async Task AddAsync(StickyMessage sticky, IGuildChannel channel)
    {
        var messages = await GetStickyMessages(channel.Guild);
        messages.Add(sticky);

        if (sticky.IsActive)
            await EnableAsync(sticky, channel);
        else
            await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(StickyMessage sticky)
    {
        var template = sticky.Template;

        _cache.Remove(template.Id);

        _db.TryRemove(template);
        _db.Remove(sticky);

        await _db.SaveChangesAsync();
    }

    public async Task DisableAsync(StickyMessage sticky)
    {
        sticky.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task EnableAsync(StickyMessage sticky, IGuildChannel channel)
    {
        await DisableStickiesAsync(channel);
        sticky.IsActive = true;

        await _db.SaveChangesAsync();
    }

    public async Task SendStickyMessage(StickyMessage sticky, ITextChannel channel)
    {
        var template = sticky.Template;
        if (!ShouldResendSticky(sticky, out var details)) return;

        if (sticky.Template.IsLive)
            await _db.UpdateAsync(template, channel.Guild);

        while (details.Messages.TryTake(out var message))
        {
            _ = message.DeleteAsync();
        }

        var options = template.ReplaceTimestamps ? EmbedBuilderOptions.ReplaceTimestamps : EmbedBuilderOptions.None;
        var embeds = template.Embeds.Select(e => e.ToBuilder(options));
        var components = template.Components.ToBuilder();

        if (details.Token.IsCancellationRequested) return;
        details.Messages.Add(await channel.SendMessageAsync(
            template.Content,
            allowedMentions: template.AllowMentions
                ? AllowedMentions.All
                : AllowedMentions.None,
            embeds: embeds.Select(e => e.Build()).ToArray(),
            components: components.Build(),
            flags: template.SuppressEmbeds ? MessageFlags.SuppressEmbeds : MessageFlags.None,
            options: new RequestOptions { CancelToken = details.Token.Token }));
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
        }) ?? throw new InvalidOperationException($"Cache entry was null in {nameof(StickyService)}");

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

    private async Task DisableStickiesAsync(IGuildChannel channel)
    {
        var stickies = await GetStickyMessages(channel.Guild);
        foreach (var sticky in stickies.Where(sticky => sticky.ChannelId == channel.Id && sticky.IsActive))
        {
            await DisableAsync(sticky);
        }
    }
}