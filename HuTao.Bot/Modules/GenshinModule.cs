using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using Hangfire;
using Hangfire.Storage;
using Humanizer;
using HuTao.Data.Models.Discord;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Utilities;
using CPreconditionAttribute = Discord.Commands.PreconditionAttribute;
using CPreconditionResult = Discord.Commands.PreconditionResult;
using IPreconditionResult = Discord.Interactions.PreconditionResult;
using IPreconditionAttribute = Discord.Interactions.PreconditionAttribute;
using Summary = Discord.Commands.SummaryAttribute;

namespace HuTao.Bot.Modules;

public class GenshinModule
{
    public static string Version = "5.1";
    public static uint VersionColor = 0xfeef5f;
    public static DateTimeOffset CodeExpiry = DateTimeOffset.Parse("9/28/2024 12:00 PM +8");

    public static DateRange Livestream = Distance(DateTimeOffset.Parse("9/27/2024 8:00 PM +8").ToUnixTimeSeconds(),
        TimeSpan.FromHours(1));

    public static string LivestreamImage = "https://pbs.twimg.com/media/GYSyTqeWoAArbKU.png:large";

    public static DateRange Maintenance = Distance(DateTimeOffset.Parse("10/9/2024 6:00 AM +8").ToUnixTimeSeconds(),
        TimeSpan.FromHours(5));

    public static string MaintenanceImage = "https://pbs.twimg.com/media/GYSyTqeWoAArbKU.png:large";
    private static readonly HttpClient Client = new();

    private static readonly Dictionary<string, string> Links = new()
    {
        ["CN→EN WFP"] = "https://twitch.tv/wangshengfp",
        ["EN KQM"]    = "https://twitch.tv/keqingmains",
        ["EN Twitch"] = "https://twitch.tv/genshinimpactofficial"
    };

    public static ConcurrentDictionary<string, int> Codes { get; } = new();

    public static HashSet<ulong> AllowedUsers { get; } =
    [
        852717789071278100 // hime.san
    ];

    public static HashSet<ulong> AllowedRoles { get; } =
    [
        784295266651471897 // Millelith
    ];

    public static DateRange Distance(long start, TimeSpan duration) => new(start, start + (long) duration.TotalSeconds);

    public static async Task ReplyCodesAsync(Context context, ITextChannel? send = null, IUserMessage? update = null)
    {
        if (Codes.IsEmpty)
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
                    embeds: [embed.Build()],
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

    [Discord.Interactions.Group("genshin", "Genshin Impact Commands")]
    public class GenshinInteractive(DiscordSocketClient client) : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("codes", true)]
        public async Task CodesAsync() => await ReplyCodesAsync(Context);

        [SlashCommand("code-add", "Add a code to the Genshin Impact codes list")]
        [RequireAllowedUserInteraction]
        public async Task AddCode(string code, int gems)
        {
            Codes.AddOrUpdate(code, gems, (_, _) => gems);
            await ReplyCodesAsync(Context);
        }

        [SlashCommand("code-remove", "Remove a code from the Genshin Impact codes list")]
        [RequireAllowedUserInteraction]
        public async Task RemoveCode(string code)
        {
            Codes.TryRemove(code, out _);
            await ReplyCodesAsync(Context);
        }

        [SlashCommand("channel-add", "Track the Genshin Impact update in a specific channel")]
        [RequireAllowedUserInteraction]
        public async Task TrackUpdate(IGuildChannel channel)
        {
            var id = $"update:{channel.Guild.Id}";

            RecurringJob.AddOrUpdate(id,
                () => UpdatePatchAsync(channel.Guild.Id, channel.Id),
                "*/5 * * * *");

            await RespondAsync($"Added {MentionUtils.MentionChannel(channel.Id)} to the update tracking list.");
        }

        [AutomaticRetry(Attempts = 0)]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public async Task UpdatePatchAsync(ulong guildId, ulong channelId)
        {
            var guild = client.GetGuild(guildId) as IGuild ?? await client.Rest.GetGuildAsync(guildId);
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

        [SlashCommand("channel-remove", "Stop tracking the Genshin Impact update in a specific channel")]
        [RequireAllowedUserInteraction]
        public async Task RemoveChannel(IGuildChannel channel)
        {
            var id = $"update:{channel.Guild.Id}";
            RecurringJob.RemoveIfExists(id);

            await RespondAsync($"Removed {MentionUtils.MentionChannel(channel.Id)} from the update tracking list.");
        }

        [SlashCommand("channel-list", "View all channels that are tracking the Genshin Impact update")]
        [RequireAllowedUserInteraction]
        public async Task ListChannels()
        {
            var allJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
            var jobs = allJobs.Where(j => j.Id.StartsWith("update:"));
            var channels = jobs.Select(j => ulong.Parse(j.Id.Split(':')[1]));

            await RespondAsync(channels.Humanize(MentionUtils.MentionChannel));
        }

        [SlashCommand("patch", "View the remaining time for Genshin's Update")]
        public async Task LivestreamAsync()
            => await RespondAsync(
                ephemeral: true,
                embed: GetEmbed(Context).Build(),
                components: GetComponents()?.Build());

        [SlashCommand("allow-user", "Allows a user to manage the Genshin Impact commands")]
        public async Task AllowAsync(IGuildUser user)
        {
            if (AllowedUsers.Add(user.Id))
                await RespondAsync($"Added {user.Mention} to allowed users.");
            else
                await RespondAsync($"{user.Mention} is already an allowed user.");
        }

        [SlashCommand("allow-role", "Allows a role to manage the Genshin Impact commands")]
        public async Task AllowAsync(IRole role)
        {
            if (AllowedRoles.Add(role.Id))
                await RespondAsync($"Added {role.Mention} to allowed roles.");
            else
                await RespondAsync($"{role.Mention} is already an allowed role.");
        }

        [SlashCommand("disallow-user", "Disallows a user from managing the Genshin Impact commands")]
        public async Task DisallowAsync(IGuildUser user)
        {
            if (AllowedUsers.Remove(user.Id))
                await RespondAsync($"Removed {user.Mention} from allowed users.");
            else
                await RespondAsync($"{user.Mention} was not an allowed user.");
        }

        [SlashCommand("disallow-role", "Disallows a role from managing the Genshin Impact commands")]
        public async Task DisallowAsync(IRole role)
        {
            if (AllowedRoles.Remove(role.Id))
                await RespondAsync($"Removed {role.Mention} from allowed roles.");
            else
                await RespondAsync($"{role.Mention} was not an allowed role.");
        }

        [SlashCommand("allowed", "View all users and roles that are allowed to manage the Genshin Impact commands")]
        public async Task AllowedAsync()
        {
            var users = AllowedUsers.Select(MentionUtils.MentionUser);
            var roles = AllowedRoles.Select(MentionUtils.MentionRole);

            var embed = new EmbedBuilder()
                .WithTitle("Allowed Users & Roles")
                .WithColor(VersionColor)
                .AddField("Users", string.Join(", ", users), true)
                .AddField("Roles", string.Join(", ", roles), true);

            await RespondAsync(embed: embed.Build());
        }

        [SlashCommand("settings", "View and modify Genshin Impact settings")]
        [RequireAllowedUserInteraction]
        public async Task SettingsAsync()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Genshin Impact Settings")
                .WithColor(VersionColor)
                .AddField("Version", Version, true)
                .AddField("Version Color", $"0x{VersionColor:X}", true)
                .AddField("Code Expiry", CodeExpiry.ToString("g"), true)
                .AddField("Livestream Start", Livestream.Start.ToString("g"), true)
                .AddField("Maintenance Start", Maintenance.Start.ToString("g"), true)
                .WithFooter(
                    "Use the button below to modify these settings.\nExamples: Version - 5.1, Color - 0xfeef5f, Expiry - 9/28/2024 12:00 PM +8");

            var components = new ComponentBuilder()
                .WithButton("Modify Settings", "modify_settings");

            await RespondAsync(embed: embed.Build(), components: components.Build(), ephemeral: true);
        }

        [ComponentInteraction("modify_settings")]
        [RequireAllowedUserInteraction]
        public async Task ModifySettingsButton()
            => await Context.Interaction.RespondWithModalAsync<GenshinSettingsModal>("settings_modal",
                modifyModal: m =>
                {
                    m.UpdateTextInput("version", x => x.Value          = Version);
                    m.UpdateTextInput("versionColor", x => x.Value     = VersionColor.ToString("X"));
                    m.UpdateTextInput("codeExpiry", x => x.Value       = CodeExpiry.ToString("g"));
                    m.UpdateTextInput("livestreamStart", x => x.Value  = Livestream.Start.ToString("g"));
                    m.UpdateTextInput("maintenanceStart", x => x.Value = Maintenance.Start.ToString("g"));
                });

        [ModalInteraction("settings_modal")]
        [RequireAllowedUserInteraction]
        public async Task OnSettingsModalSubmit(GenshinSettingsModal modal)
        {
            // Validate Version (cannot be empty)
            if (string.IsNullOrWhiteSpace(modal.Version))
            {
                await RespondAsync("Version cannot be empty.", ephemeral: true);
                return;
            }

            // Validate VersionColor as hex (e.g. "0xfeef5f")
            var colorInput = modal.VersionColor.Trim().ToLower();
            if (colorInput.StartsWith("0x"))
                colorInput = colorInput.Substring(2);
            if (!uint.TryParse(colorInput, NumberStyles.HexNumber, null, out var parsedColor))
            {
                await RespondAsync("Invalid Version Color. Please follow the format e.g. 0xfeef5f", ephemeral: true);
                return;
            }

            // Validate CodeExpiry (e.g. "9/28/2024 12:00 PM +8")
            if (!DateTimeOffset.TryParse(modal.CodeExpiry, out var parsedExpiry))
            {
                await RespondAsync("Invalid Code Expiry. Please follow the format e.g. 9/28/2024 12:00 PM +8",
                    ephemeral: true);
                return;
            }

            // Validate LivestreamStart (e.g. "9/27/2024 8:00 PM +8")
            if (!DateTimeOffset.TryParse(modal.LivestreamStart, out var parsedLivestream))
            {
                await RespondAsync("Invalid Livestream Start. Please follow the format e.g. 9/27/2024 8:00 PM +8",
                    ephemeral: true);
                return;
            }

            // Validate MaintenanceStart (e.g. "10/9/2024 6:00 AM +8")
            if (!DateTimeOffset.TryParse(modal.MaintenanceStart, out var parsedMaintenance))
            {
                await RespondAsync("Invalid Maintenance Start. Please follow the format e.g. 10/9/2024 6:00 AM +8",
                    ephemeral: true);
                return;
            }

            // Update settings. (For the date ranges we keep the previously set durations)
            Version      = modal.Version.Trim();
            VersionColor = parsedColor;
            CodeExpiry   = parsedExpiry;

            var oldLivestreamDuration = Livestream.End - Livestream.Start;
            Livestream = Distance(parsedLivestream.ToUnixTimeSeconds(), oldLivestreamDuration);

            var oldMaintenanceDuration = Maintenance.End - Maintenance.Start;
            Maintenance = Distance(parsedMaintenance.ToUnixTimeSeconds(), oldMaintenanceDuration);

            await RespondAsync("Settings updated successfully.", ephemeral: true);
        }
    }

    public class GenshinCommands(DiscordSocketClient client) : ModuleBase<SocketCommandContext>
    {
        [Command("genshin allow")]
        [RequireAllowedUserCommand]
        [Summary("Allows a user or role to manage the Genshin Impact commands")]
        public async Task AllowUser(IGuildUser user)
        {
            if (AllowedUsers.Add(user.Id))
                await ReplyAsync($"Added {user.Mention} to allowed users.");
            else
                await ReplyAsync($"{user.Mention} is already an allowed user.");
        }

        [Command("genshin allow")]
        [RequireAllowedUserCommand]
        [Summary("Allows a role to manage the Genshin Impact commands")]
        public async Task AllowRole(IRole role)
        {
            if (AllowedRoles.Add(role.Id))
                await ReplyAsync($"Added {role.Mention} to allowed roles.");
            else
                await ReplyAsync($"{role.Mention} is already an allowed role.");
        }

        [Command("genshin disallow")]
        [RequireAllowedUserCommand]
        [Summary("Disallows a user from managing the Genshin Impact commands")]
        public async Task DisallowUser(IGuildUser user)
        {
            if (AllowedUsers.Remove(user.Id))
                await ReplyAsync($"Removed {user.Mention} from allowed users.");
            else
                await ReplyAsync($"{user.Mention} was not an allowed user.");
        }

        [Command("genshin disallow")]
        [RequireAllowedUserCommand]
        [Summary("Disallows a role from managing the Genshin Impact commands")]
        public async Task DisallowRole(IRole role)
        {
            if (AllowedRoles.Remove(role.Id))
                await ReplyAsync($"Removed {role.Mention} from allowed roles.");
            else
                await ReplyAsync($"{role.Mention} was not an allowed role.");
        }

        [Command("genshin allowed")]
        [RequireAllowedUserCommand]
        [Summary("View all users and roles that are allowed to manage the Genshin Impact commands")]
        public async Task ListAllowedUsers()
        {
            var users = AllowedUsers.Select(MentionUtils.MentionUser);
            var roles = AllowedRoles.Select(MentionUtils.MentionRole);

            var embed = new EmbedBuilder()
                .WithTitle("Allowed Users & Roles")
                .WithColor(VersionColor)
                .AddField("Users", string.Join(", ", users), true)
                .AddField("Roles", string.Join(", ", roles), true);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("genshin settings")]
        [RequireAllowedUserCommand]
        [Summary("View and modify Genshin Impact settings")]
        public async Task SettingsAsync()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Genshin Impact Settings")
                .WithColor(VersionColor)
                .AddField("Version", Version, true)
                .AddField("Version Color", $"0x{VersionColor:X}", true)
                .AddField("Code Expiry", CodeExpiry.ToString("g"), true)
                .AddField("Livestream Start", Livestream.Start.ToString("g"), true)
                .AddField("Maintenance Start", Maintenance.Start.ToString("g"), true)
                .WithFooter(
                    "Use the button below to modify these settings.\nExamples: Version - 5.1, Color - 0xfeef5f, Expiry - 9/28/2024 12:00 PM +8");

            var components = new ComponentBuilder()
                .WithButton("Modify Settings", "modify_settings");

            await ReplyAsync(embed: embed.Build(), components: components.Build());
        }

        [Command("code add")]
        [Alias("codes add")]
        [RequireAllowedUserCommand]
        [Summary("Adds a code to the Genshin Impact codes list")]
        public async Task AddCode(string code, int gems)
        {
            Codes.AddOrUpdate(code, gems, (_, _) => gems);
            await ReplyCodesAsync(Context);
        }

        [Command("code")]
        [Alias("codes")]
        [Summary("View the Genshin Impact codes list")]
        public Task CodesAsync(ITextChannel? channel = null)
            => ReplyCodesAsync(Context, channel);

        [Command("code")]
        [Alias("codes")]
        [Summary("View the Genshin Impact codes list")]
        public Task CodesAsync(IUserMessage? message = null)
            => ReplyCodesAsync(Context, message?.Channel as ITextChannel, message);

        [Command("livestream")]
        [Alias("live", "stream", "update", "patch")]
        [Summary("View the remaining time for Genshin's {Version} Update")]
        public async Task LivestreamAsync() => await ReplyAsync(
            embed: GetEmbed(Context).Build(),
            components: GetComponents()?.Build());

        [Command("code remove")]
        [Alias("codes remove")]
        [RequireAllowedUserCommand]
        [Summary("Removes a code from the Genshin Impact codes list")]
        public Task RemoveCode(string code) => Codes.TryRemove(code, out _)
            ? ReplyCodesAsync(Context)
            : ReplyAsync("Code not found.");
    }

    private class RequireAllowedUserCommandAttribute : CPreconditionAttribute
    {
        public override async Task<CPreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            if (context.User is not IGuildUser user)
                return CPreconditionResult.FromError("This command can only be used in a guild.");

            if (context.Client.TokenType is not TokenType.Bot)
            {
                return CPreconditionResult.FromError(
                    $"{nameof(RequireTeamMemberAttribute)} is not supported by this TokenType.");
            }

            var application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

            if (context.User.Id == application.Owner.Id
                || context.User.Id == application.Team.OwnerUserId
                || application.Team.TeamMembers.Any(t => context.User.Id == t.User.Id))
                return CPreconditionResult.FromSuccess();

            return AllowedUsers.Contains(user.Id) || user.RoleIds.Any(AllowedRoles.Contains)
                ? CPreconditionResult.FromSuccess()
                : CPreconditionResult.FromError("You are not authorized to use this command.");
        }
    }

    private class RequireAllowedUserInteractionAttribute : IPreconditionAttribute
    {
        public override async Task<IPreconditionResult> CheckRequirementsAsync(
            IInteractionContext context,
            ICommandInfo commandInfo, IServiceProvider services)
        {
            if (context.User is not IGuildUser user)
                return IPreconditionResult.FromError("This command can only be used in a guild.");

            if (context.Client.TokenType is not TokenType.Bot)
            {
                return IPreconditionResult.FromError(
                    $"{nameof(RequireTeamMemberAttribute)} is not supported by this TokenType.");
            }

            var application = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

            if (context.User.Id == application.Owner.Id
                || context.User.Id == application.Team.OwnerUserId
                || application.Team.TeamMembers.Any(t => context.User.Id == t.User.Id))
                return IPreconditionResult.FromSuccess();

            return AllowedUsers.Contains(user.Id) || user.RoleIds.Any(AllowedRoles.Contains)
                ? IPreconditionResult.FromSuccess()
                : IPreconditionResult.FromError("You are not authorized to use this command.");
        }
    }

    public class GenshinSettingsModal : IModal
    {
        [InputLabel("Version")]
        [ModalTextInput("version", TextInputStyle.Short, "e.g. 5.1", maxLength: 10)]
        public string Version { get; set; }

        [InputLabel("Version Color (Hex)")]
        [ModalTextInput("versionColor", TextInputStyle.Short, "e.g. 0xfeef5f", maxLength: 10)]
        public string VersionColor { get; set; }

        [InputLabel("Code Expiry")]
        [ModalTextInput("codeExpiry", TextInputStyle.Short, "e.g. 9/28/2024 12:00 PM +8", maxLength: 30)]
        public string CodeExpiry { get; set; }

        [InputLabel("Livestream Start")]
        [ModalTextInput("livestreamStart", TextInputStyle.Short, "e.g. 9/27/2024 8:00 PM +8", maxLength: 30)]
        public string LivestreamStart { get; set; }

        [InputLabel("Maintenance Start")]
        [ModalTextInput("maintenanceStart", TextInputStyle.Short, "e.g. 10/9/2024 6:00 AM +8", maxLength: 30)]
        public string MaintenanceStart { get; set; }

        // Setting the CustomId to be matched by the ModalInteraction handler.
        public string Title => "Modify Genshin Settings";
    }
}