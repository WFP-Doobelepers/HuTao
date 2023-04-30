using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using Hangfire;
using Humanizer;
using HuTao.Data.Models.Discord;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules;

public class GenshinModule
{
    private const string Version = "3.4";
    private const int VersionColor = 0x864646;
    public static DateTimeOffset CodeExpiry = DateTimeOffset.FromUnixTimeSeconds(1673064000);
    public static DateRange Livestream = new(1673006400, 1673010000);
    public static string LivestreamImage = "https://pbs.twimg.com/media/FlmYpMXXkAESPtf.png:large";
    public static DateRange Maintenance = new(1673992800, 1674010800);
    public static string MaintenanceImage = "https://pbs.twimg.com/media/FlmYpMXXkAESPtf.png:large";
    private static readonly HttpClient Client = new();

    private static readonly Dictionary<string, string> Links = new()
    {
        ["CN→EN WFP"] = "https://twitch.tv/wangshengfp",
        ["EN KQM"]    = "https://twitch.tv/keqingmains",
        ["EN Twitch"] = "https://twitch.tv/genshinimpactofficial"
    };

    public static ConcurrentDictionary<string, int> Codes { get; } = new();

    public static async Task ReplyCodesAsync(Context context, ITextChannel? send = null, IUserMessage? update = null)
    {
        if (!Codes.Any())
        {
            await context.ReplyAsync("There are no codes available yet.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Genshin Impact {Version} Codes")
            .WithColor(0xD1DCF3)
            .WithDescription(string.Join(Environment.NewLine, Codes.Select(c =>
                $"{c.Value} <:E_Primogem:798510052583014400> " +
                $"**[{c.Key}](https://genshin.hoyoverse.com/en/gift?code={c.Key})**")))
            .WithThumbnailUrl(context.Guild.Id == 791074691841523742 ? LivestreamImage : MaintenanceImage)
            .WithFooter("Codes expires in").WithTimestamp(CodeExpiry);

        var components = new ComponentBuilder();

        components.WithButton("Redeem ⇾", "codes", ButtonStyle.Success, disabled: true);
        foreach (var code in Codes)
        {
            components.WithButton(
                $"{code.Value} {code.Key}",
                style: ButtonStyle.Link,
                emote: Emote.Parse("<:E_Primogem:798510052583014400>"),
                url: $"https://genshin.hoyoverse.com/en/gift?code={code.Key}");
        }

        if (send is not null && context.User is IGuildUser user)
        {
            var permissions = user.GetPermissions(send);
            if (!permissions.Has(ChannelPermission.SendMessages))
            {
                await context.ReplyAsync("You don't have permission to send messages in here.", ephemeral: true);
                return;
            }

            var webhooks = await send.GetWebhooksAsync();
            var webhook = webhooks.FirstOrDefault(w => w.Creator.Id == context.Client.CurrentUser.Id);
            if (webhook is null)
            {
                var stream = await Client.GetStreamAsync(context.Client.CurrentUser.GetAvatarUrl(size: 4096));
                webhook = await send.CreateWebhookAsync("Genshin Impact Codes", stream);
            }

            var client = new DiscordWebhookClient(webhook);
            if (update is not null)
            {
                await client.ModifyMessageAsync(update.Id, m =>
                {
                    m.Embeds     = new[] { embed.Build() };
                    m.Components = components.Build();
                });
            }
            else
            {
                await client.SendMessageAsync(
                    embeds: new[] { embed.Build() },
                    components: components.Build());
            }
        }
        else
        {
            await context.ReplyAsync(
                embed: embed.Build(),
                components: components.Build(),
                ephemeral: true);
        }
    }

    private static ComponentBuilder? GetComponents()
    {
        var components = new ComponentBuilder().WithButton("View codes", "codes", ButtonStyle.Success);
        if (DateTimeOffset.Now >= Livestream.End)
            return components;

        foreach (var link in Links)
        {
            components.WithButton(link.Key,
                style: ButtonStyle.Link,
                url: link.Value);
        }

        return components;
    }

    private static EmbedBuilder GetEmbed(Context context) => Livestream.End > DateTimeOffset.Now
        ? new EmbedBuilder()
            .WithTitle($"Version {Version} Special Program Preview 📣")
            .WithColor(VersionColor)
            .AddField("Livestream Starts", Livestream.Start.ToUniversalTimestamp(), true)
            .AddField("Maintenance Starts", Maintenance.Start.ToUniversalTimestamp(), true)
            .WithImageUrl(LivestreamImage)
        : new EmbedBuilder()
            .WithTitle($"Version {Version} Update Maintenance")
            .WithColor(VersionColor)
            .AddField("Maintenance Starts", Maintenance.Start.ToUniversalTimestamp(), true)
            .AddField("Maintenance Ends", Maintenance.End.ToUniversalTimestamp(), true)
            .WithImageUrl(MaintenanceImage);

    private static string Link(string name, string url) => Format.Bold($"[{name}](https://{url})");

    public record DateRange(DateTimeOffset Start, DateTimeOffset End)
    {
        public DateRange(long start, long end) : this(
            DateTimeOffset.FromUnixTimeSeconds(start),
            DateTimeOffset.FromUnixTimeSeconds(end)) { }
    }

    public class GenshinInteractive : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("patch", $"View the remaining time for Genshin's {Version} Update")]
        public async Task LivestreamAsync()
            => await RespondAsync(
                ephemeral: true,
                embed: GetEmbed(Context).Build(),
                components: GetComponents()?.Build());

        [ComponentInteraction("codes")]
        public async Task CodesAsync() => await ReplyCodesAsync(Context);
    }

    public class GenshinCommands : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;

        public GenshinCommands(DiscordSocketClient client) { _client = client; }

        [Command("code add")]
        [RequireTeamMember]
        public async Task AddCode(string code, int gems)
        {
            Codes.AddOrUpdate(code, gems, (_, _) => gems);
            await ReplyCodesAsync(Context);
        }

        [Command("code")]
        [Alias("codes")]
        public Task CodesAsync(ITextChannel? channel = null)
            => ReplyCodesAsync(Context, channel);

        [Command("code")]
        [Alias("codes")]
        public Task CodesAsync(IUserMessage? message = null)
            => ReplyCodesAsync(Context, message?.Channel as ITextChannel, message);

        [Command("livestream")]
        [Alias("live", "stream", "update", "patch")]
        [Discord.Commands.Summary($"View the remaining time for Genshin's {Version} Update")]
        public async Task LivestreamAsync() => await ReplyAsync(
            embed: GetEmbed(Context).Build(),
            components: GetComponents()?.Build());

        [Command("code remove")]
        [RequireTeamMember]
        public Task RemoveCode(string code) => Codes.TryRemove(code, out _)
            ? ReplyCodesAsync(Context)
            : ReplyAsync("Code not found.");

        [Command("code channel")]
        [RequireTeamMember]
        public async Task TrackUpdate(IGuildChannel channel)
        {
            var id = $"update:{channel.Guild.Id}";

            RecurringJob.AddOrUpdate(id,
                () => UpdatePatchAsync(channel.Guild.Id, channel.Id),
                "*/5 * * * *");

            await ReplyAsync("Done");
        }

        [AutomaticRetry(Attempts = 0)]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public async Task UpdatePatchAsync(ulong guildId, ulong channelId)
        {
            var guild = await GetGuildAsync(guildId);
            if (guild is null) return;

            var channel = await guild.GetChannelAsync(channelId);
            if (channel is null) return;

            var name = DateTimeOffset.Now switch
            {
                var e when Livestream.Start > e  => $"{Version} Stream {Livestream.Start.TimeLeft().Humanize(2)}",
                var e when Livestream.End > e    => $"{Version} Stream Started {Livestream.Start.Humanize()}",
                var e when Maintenance.Start > e => $"Maint Start {Maintenance.Start.TimeLeft().Humanize(2)}",
                var e when Maintenance.End > e   => $"Maint Ends {Maintenance.End.TimeLeft().Humanize(2)}",
                var e when e > Livestream.End    => $"{Version} Maintenance Ended!",
                _                                => "IDK, hime broke the dates"
            };

            await channel.ModifyAsync(c => c.Name = name, new RequestOptions
            {
                Timeout = (int) TimeSpan.FromSeconds(30).TotalMilliseconds
            });
        }

        private async Task<IGuild?> GetGuildAsync(ulong guildId) =>
            _client.GetGuild(guildId) as IGuild
            ?? await _client.Rest.GetGuildAsync(guildId);
    }
}