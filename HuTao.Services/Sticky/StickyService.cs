using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.Linking;
using HuTao.Services.Utilities;
using Microsoft.Extensions.Caching.Memory;

namespace HuTao.Services.Sticky;

public class StickyService(IMemoryCache cache, HuTaoContext db)
{
    public async Task AddAsync(StickyMessage sticky, IGuildChannel channel)
    {
        var messages = await GetStickyMessages(channel.Guild);
        messages.Add(sticky);

        if (sticky.IsActive)
            await EnableAsync(sticky, channel);
        else
            await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(StickyMessage sticky)
    {
        var template = sticky.Template;

        cache.Remove(template.Id);

        db.TryRemove(template);
        db.Remove(sticky);

        await db.SaveChangesAsync();
    }

    public async Task DisableAsync(StickyMessage sticky)
    {
        sticky.IsActive = false;
        await db.SaveChangesAsync();
    }

    public async Task EnableAsync(StickyMessage sticky, IGuildChannel channel)
    {
        await DisableStickiesAsync(channel);
        sticky.IsActive = true;

        await db.SaveChangesAsync();
    }

    public async Task SendStickyMessage(StickyMessage sticky, ITextChannel channel)
    {
        var template = sticky.Template;
        if (!ShouldResendSticky(sticky, out var details)) return;

        if (sticky.Template.IsLive)
            await db.UpdateAsync(template, channel.Guild);

        while (details.Messages.TryTake(out var message))
        {
            _ = message.DeleteAsync();
        }

        var allowedMentions = template.AllowMentions ? AllowedMentions.All : AllowedMentions.None;
        var flags = template.SuppressEmbeds ? MessageFlags.SuppressEmbeds : MessageFlags.None;

        var embeds = template.GetEmbedBuilders()
            .Select(e => e.Build())
            .ToList();

        const uint defaultAccentColor = 0x9B59FF;
        var builder = new ComponentBuilderV2();

        if (!string.IsNullOrWhiteSpace(template.Content))
        {
            builder.WithContainer(new ContainerBuilder()
                .WithTextDisplay(template.Content.Truncate(4000))
                .WithAccentColor(defaultAccentColor));
        }

        foreach (var embed in embeds)
            builder.WithContainer(embed.ToComponentsV2Container());

        foreach (var row in template.Components.ToActionRowBuilders())
            builder.WithActionRow(row);

        if (string.IsNullOrWhiteSpace(template.Content) && embeds.Count == 0)
        {
            builder.WithContainer(new ContainerBuilder()
                .WithTextDisplay("-# (empty template)")
                .WithAccentColor(defaultAccentColor));
        }

        if (details.Token.IsCancellationRequested) return;
        details.Messages.Add(await channel.SendMessageAsync(
            allowedMentions: allowedMentions,
            components: builder.Build(),
            flags: flags,
            options: new RequestOptions { CancelToken = details.Token.Token }));
    }

    public async Task<ICollection<StickyMessage>> GetStickyMessages(IGuild guild)
    {
        var entity = await db.Guilds.TrackGuildAsync(guild);
        return entity.StickyMessages;
    }

    private bool ShouldResendSticky(StickyMessage sticky, out StickyMessageDetails details)
    {
        details = cache.GetOrCreate(sticky.Id, e =>
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