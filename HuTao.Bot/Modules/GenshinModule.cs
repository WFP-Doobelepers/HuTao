using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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
    public static string Version = "Luna I (6.0)";
    public static uint VersionColor = 0x483C7E;
    public static DateTimeOffset CodeExpiry = DateTimeOffset.Parse("09/02/2025 12:00 AM +8");

    public static TimeSpan LivestreamDuration = TimeSpan.FromHours(1);

    public static DateRange Livestream = Distance(DateTimeOffset.Parse("8/29/2025 11:51 PM +8").ToUnixTimeSeconds(),
        LivestreamDuration);

    public static string LivestreamImage = "https://pbs.twimg.com/media/GzVIcBMW8AAUMFM.png:large";

    public static TimeSpan MaintenanceDuration = TimeSpan.FromHours(5);

    public static DateRange Maintenance = Distance(DateTimeOffset.Parse("09/10/2025 6:00 AM +8").ToUnixTimeSeconds(),
        MaintenanceDuration);

    public static string MaintenanceImage = "https://pbs.twimg.com/media/GzVIcBMW8AAUMFM.png:large";
    private static readonly HttpClient Client = new();

    private static readonly Dictionary<string, string> Links = new()
    {
        ["CN→EN WFP"] = "https://twitch.tv/wangshengfp",
        ["EN KQM"]    = "https://twitch.tv/keqingmains",
        ["EN Twitch"] = "https://twitch.tv/genshinimpactofficial"
    };

    public static ConcurrentDictionary<string, int> Codes { get; } = new()
    {
        ["LunaI0910"]         = 100,
        ["LaumaNodKraiFlins"] = 100,
        ["HiFiveTraveler"]    = 100
    };

    public static HashSet<ulong> AllowedRoles { get; } =
    [
        784295266651471897,
        791662675516063754,
        1419299050712334389,
        1429503758538244096,
        1438774324554104943,
        1457538938519552085,
        1457539448026960104,
        1457539478611824844,
        1457539506356883547,
        1206297856596508764,
        1439736280828088341
    ];

    public static HashSet<ulong> AllowedUsers { get; } =
    [
        852717789071278100,
        925534335434653736,
        1387820807358644238
    ];

    public static DateRange Distance(long start, TimeSpan duration) => new(start, start + (long) duration.TotalSeconds);

    public class VersionData
    {
        public string Name { get; set; } = "";
        public uint Color { get; set; } = 0x483C7E;
        public DateTimeOffset LivestreamStart { get; set; }
        public DateTimeOffset MaintenanceStart { get; set; }
        public TimeSpan LivestreamDuration { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan MaintenanceDuration { get; set; } = TimeSpan.FromHours(5);
        public string? LivestreamImage { get; set; }
        public string? MaintenanceImage { get; set; }
        public DateTimeOffset? CodeExpiry { get; set; }

        [JsonIgnore] public DateTimeOffset EffectiveCodeExpiry => CodeExpiry ?? LivestreamStart.AddDays(3);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Confirmed versions use actual announced dates.
    // Projected versions follow the consistent 42-day cycle:
    //   Maintenance: Wednesday at 6:00 AM UTC+8
    //   Livestream: Friday 12 days prior at 8:00 PM UTC+8
    public static List<VersionData> AllVersions { get; set; } =
    [
        new() { Name = "Luna I (6.0)", Color = 0x483C7E,
            LivestreamStart = DateTimeOffset.Parse("08/29/2025 8:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("09/10/2025 6:00 AM +8"),
            LivestreamImage = "https://static.wikia.nocookie.net/gensin-impact/images/c/ca/Splashscreen_A_Dance_of_Snowy_Tides_and_Hoarfrost_Groves.png",
            MaintenanceImage = "https://static.wikia.nocookie.net/gensin-impact/images/c/ca/Splashscreen_A_Dance_of_Snowy_Tides_and_Hoarfrost_Groves.png" },
        new() { Name = "Luna II (6.1)",
            LivestreamStart = DateTimeOffset.Parse("10/10/2025 8:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("10/22/2025 6:00 AM +8"),
            LivestreamImage = "https://static.wikia.nocookie.net/gensin-impact/images/1/1d/Splashscreen_An_Elegy_for_Faded_Moonlight.png",
            MaintenanceImage = "https://static.wikia.nocookie.net/gensin-impact/images/1/1d/Splashscreen_An_Elegy_for_Faded_Moonlight.png" },
        new() { Name = "Luna III (6.2)",
            LivestreamStart = DateTimeOffset.Parse("11/21/2025 8:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("12/03/2025 6:00 AM +8"),
            LivestreamImage = "https://static.wikia.nocookie.net/gensin-impact/images/4/41/Splashscreen_A_Nocturne_of_the_Far_North.png",
            MaintenanceImage = "https://static.wikia.nocookie.net/gensin-impact/images/4/41/Splashscreen_A_Nocturne_of_the_Far_North.png" },
        new() { Name = "Luna IV (6.3)",
            LivestreamStart = DateTimeOffset.Parse("01/02/2026 1:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("01/14/2026 6:00 AM +8"),
            LivestreamImage = "https://static.wikia.nocookie.net/gensin-impact/images/c/c4/Splashscreen_A_Traveler_on_a_Winter%27s_Night.png",
            MaintenanceImage = "https://static.wikia.nocookie.net/gensin-impact/images/c/c4/Splashscreen_A_Traveler_on_a_Winter%27s_Night.png" },
        new() { Name = "Luna V (6.4)",
            LivestreamStart = DateTimeOffset.Parse("02/13/2026 8:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("02/25/2026 6:00 AM +8"),
            LivestreamImage = "https://static.wikia.nocookie.net/gensin-impact/images/2/24/Splashscreen_Homeward%2C_He_Who_Caught_the_Wind.png",
            MaintenanceImage = "https://static.wikia.nocookie.net/gensin-impact/images/2/24/Splashscreen_Homeward%2C_He_Who_Caught_the_Wind.png" },
        new() { Name = "Luna VI (6.5)",
            LivestreamStart = DateTimeOffset.Parse("03/27/2026 8:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("04/08/2026 6:00 AM +8") },
        new() { Name = "Luna VII (6.6)",
            LivestreamStart = DateTimeOffset.Parse("05/08/2026 8:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("05/20/2026 6:00 AM +8") },
        new() { Name = "Luna VIII (6.7)",
            LivestreamStart = DateTimeOffset.Parse("06/19/2026 8:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("07/01/2026 6:00 AM +8") },
        new() { Name = "Luna IX (6.8)",
            LivestreamStart = DateTimeOffset.Parse("07/31/2026 8:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("08/12/2026 6:00 AM +8") },
        new() { Name = "7.0",
            LivestreamStart = DateTimeOffset.Parse("09/11/2026 8:00 PM +8"),
            MaintenanceStart = DateTimeOffset.Parse("09/23/2026 6:00 AM +8") }
    ];

    public static int CurrentVersionIndex { get; set; }

    static GenshinModule()
    {
        var now = DateTimeOffset.UtcNow;
        for (var i = AllVersions.Count - 1; i >= 0; i--)
        {
            if (AllVersions[i].MaintenanceStart <= now)
            {
                CurrentVersionIndex = i;
                SyncToActive();
                return;
            }
        }
        CurrentVersionIndex = 0;
        SyncToActive();
    }

    public static void LoadVersion(int index)
    {
        if (index < 0 || index >= AllVersions.Count) return;
        CurrentVersionIndex = index;
        SyncToActive();
        Codes.Clear();
    }

    public static void SyncToActive()
    {
        var data = AllVersions[CurrentVersionIndex];
        Version = data.Name;
        VersionColor = data.Color;
        CodeExpiry = data.EffectiveCodeExpiry;
        LivestreamDuration = data.LivestreamDuration;
        Livestream = Distance(data.LivestreamStart.ToUnixTimeSeconds(), data.LivestreamDuration);
        MaintenanceDuration = data.MaintenanceDuration;
        Maintenance = Distance(data.MaintenanceStart.ToUnixTimeSeconds(), data.MaintenanceDuration);
        if (data.LivestreamImage is not null) LivestreamImage = data.LivestreamImage;
        if (data.MaintenanceImage is not null) MaintenanceImage = data.MaintenanceImage;
    }

    public static (int Index, VersionData? Data) ResolveTarget(int target)
    {
        var idx = target < 0 ? CurrentVersionIndex : target;
        return idx >= 0 && idx < AllVersions.Count ? (idx, AllVersions[idx]) : (-1, null);
    }

    public static VersionData? GetNextVersion()
        => CurrentVersionIndex + 1 < AllVersions.Count ? AllVersions[CurrentVersionIndex + 1] : null;

    public static string ExportJson()
    {
        var export = new { currentIndex = CurrentVersionIndex, versions = AllVersions };
        return JsonSerializer.Serialize(export, JsonOptions);
    }

    public static bool ImportJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("versions", out var versionsEl))
            {
                var versions = JsonSerializer.Deserialize<List<VersionData>>(versionsEl.GetRawText(), JsonOptions);
                if (versions is { Count: > 0 }) AllVersions = versions;
            }

            if (root.TryGetProperty("currentIndex", out var idxEl))
            {
                var idx = idxEl.GetInt32();
                if (idx >= 0 && idx < AllVersions.Count) CurrentVersionIndex = idx;
            }

            SyncToActive();
            return true;
        }
        catch { return false; }
    }

    public static async Task ReplyCodesAsync(Context context, ITextChannel? send = null, IUserMessage? update = null)
    {
        if (Codes.IsEmpty)
        {
            var noCodesComponents = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay("## Genshin Codes\nThere are no codes available yet.")
                    .WithAccentColor(VersionColor))
                .Build();

            await context.ReplyAsync(components: noCodesComponents, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        var imageUrl = context.Guild.Id == 791074691841523742 ? LivestreamImage : MaintenanceImage;
        var container = new ContainerBuilder();

        container.WithSection([
            new TextDisplayBuilder($"""
                                    ## 🎮 Genshin Impact {Version} Codes
                                    **Expires:** {CodeExpiry:f}
                                    """)
        ], new ThumbnailBuilder(new UnfurledMediaItemProperties(imageUrl)));

        container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

        var primo = Emote.Parse("<:E_Primogem:798510052583014400>");
        var codesList = string.Join("\n", Codes.Select(c =>
            $"{primo} **{c.Value}** primogems • **[{c.Key}](https://genshin.hoyoverse.com/en/gift?code={c.Key})**"));

        container.WithTextDisplay(codesList);
        container.WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small);
        container.WithTextDisplay("-# Click the buttons below to redeem codes directly");

        // Build action rows for buttons (outside container)
        var actionRow = new ActionRowBuilder()
            .WithButton("Redeem ⇾", "codes", ButtonStyle.Success, disabled: true);

        foreach (var code in Codes.Take(4))
        {
            actionRow.WithButton(new ButtonBuilder()
                .WithLabel($"{code.Value} {code.Key}")
                .WithEmote(primo)
                .WithStyle(ButtonStyle.Link)
                .WithUrl($"https://genshin.hoyoverse.com/en/gift?code={code.Key}"));
        }

        ActionRowBuilder? secondRow = null;
        if (Codes.Count > 4)
        {
            secondRow = new ActionRowBuilder();
            foreach (var code in Codes.Skip(4).Take(4))
            {
                secondRow.WithButton(new ButtonBuilder()
                    .WithLabel($"{code.Value} {code.Key}")
                    .WithEmote(primo)
                    .WithStyle(ButtonStyle.Link)
                    .WithUrl($"https://genshin.hoyoverse.com/en/gift?code={code.Key}"));
            }
        }

        container.WithAccentColor(VersionColor);

        // Build components with container and separate action rows
        var componentBuilder = new ComponentBuilderV2()
            .WithContainer(container)
            .WithActionRow(actionRow);

        if (secondRow != null) componentBuilder.WithActionRow(secondRow);

        var components = componentBuilder.Build();

        if (send is not null && context.User is IGuildUser user)
        {
            var permissions = user.GetPermissions(send);
            if (!permissions.Has(ChannelPermission.SendMessages))
            {
                var noPermComponents = new ComponentBuilderV2()
                    .WithContainer(new ContainerBuilder()
                        .WithTextDisplay("## Genshin Codes\nYou don't have permission to send messages in that channel.")
                        .WithAccentColor(VersionColor))
                    .Build();

                await context.ReplyAsync(components: noPermComponents, ephemeral: true, allowedMentions: AllowedMentions.None);
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
                await client.ModifyMessageAsync(update.Id, m => { m.Components = components; });
            else
                await client.SendMessageAsync(components: components);
        }
        else
            await context.ReplyAsync(components: components, ephemeral: true);
    }

    private static ActionRowBuilder GetActionRow()
    {
        var actionRow = new ActionRowBuilder().WithButton("View codes", "codes", ButtonStyle.Success);
        if (DateTimeOffset.Now >= Livestream.End)
            return actionRow;

        foreach (var link in Links)
        {
            actionRow.WithButton(link.Key,
                style: ButtonStyle.Link,
                url: link.Value);
        }

        return actionRow;
    }

    private static ContainerBuilder GetLivestreamContainer() => Livestream.End > DateTimeOffset.Now
        ? new ContainerBuilder()
            .WithTextDisplay(
                $"""
                 ## Version {Version} Special Program Preview 📣
                 **Livestream Starts:** <t:{Livestream.Start.ToUnixTimeSeconds()}:F> (<t:{Livestream.Start.ToUnixTimeSeconds()}:R>)
                 **Maintenance Starts:** <t:{Maintenance.Start.ToUnixTimeSeconds()}:F> (<t:{Maintenance.Start.ToUnixTimeSeconds()}:R>)
                 """)
            .WithMediaGallery([new MediaGalleryItemProperties(new UnfurledMediaItemProperties(LivestreamImage), "Special Program Preview")])
        : new ContainerBuilder()
            .WithTextDisplay(
                $"""
                 ## Version {Version} Update Maintenance
                 **Maintenance Starts:** <t:{Maintenance.Start.ToUnixTimeSeconds()}:F> (<t:{Maintenance.Start.ToUnixTimeSeconds()}:R>)
                 **Maintenance Ends:** <t:{Maintenance.End.ToUnixTimeSeconds()}:F> (<t:{Maintenance.End.ToUnixTimeSeconds()}:R>)
                 """)
            .WithMediaGallery([new MediaGalleryItemProperties(new UnfurledMediaItemProperties(MaintenanceImage), "Update Maintenance")]);

    private static MessageComponent BuildSettingsComponents() => new ComponentBuilderV2()
        .WithContainer(new ContainerBuilder()
            .WithTextDisplay(
                $"""
                 ## 🎮 Genshin Impact Settings
                 **Version:** {Version}
                 **Version Color:** 0x{VersionColor:X}
                 **Code Expiry:** <t:{CodeExpiry.ToUnixTimeSeconds()}:F> (<t:{CodeExpiry.ToUnixTimeSeconds()}:R>)
                 """))
        .WithContainer(new ContainerBuilder()
            .WithTextDisplay(
                $"""
                 ### 📺 Livestream Settings
                 **Start Time:** <t:{Livestream.Start.ToUnixTimeSeconds()}:F> (<t:{Livestream.Start.ToUnixTimeSeconds()}:R>)
                 **Duration:** {LivestreamDuration.Humanize()}
                 """)
            .WithMediaGallery([new MediaGalleryItemProperties(new UnfurledMediaItemProperties(LivestreamImage), "Livestream Image")]))
        .WithContainer(new ContainerBuilder()
            .WithTextDisplay(
                $"""
                 ### 🔧 Maintenance Settings
                 **Start Time:** <t:{Maintenance.Start.ToUnixTimeSeconds()}:F> (<t:{Maintenance.Start.ToUnixTimeSeconds()}:R>)
                 **Duration:** {MaintenanceDuration.Humanize()}
                 """)
            .WithMediaGallery([new MediaGalleryItemProperties(new UnfurledMediaItemProperties(MaintenanceImage), "Maintenance Image")]))
        .WithTextDisplay("-# Use the button below to modify these settings • Examples: Version - 5.1, Color - 0xfeef5f, Duration - 1:30")
        .WithActionRow(new ActionRowBuilder()
            .WithButton("Modify Settings", "modify_settings"))
        .Build();

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
        [SlashCommand("code-add", "Add a code to the Genshin Impact codes list")]
        [RequireAllowedUserInteraction]
        public async Task AddCode(string code, int gems)
        {
            Codes.AddOrUpdate(code, gems, (_, _) => gems);
            await ReplyCodesAsync(Context);
        }

        [SlashCommand("codes", "View the current Genshin Impact codes")]
        public async Task CodesListAsync() => await ReplyCodesAsync(Context);

        [SlashCommand("code-clear", "Clear all codes from the list")]
        [RequireAllowedUserInteraction]
        public async Task ClearCodes()
        {
            Codes.Clear();
            var components = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay("## Genshin Codes\nAll codes have been cleared.")
                    .WithAccentColor(VersionColor))
                .Build();
            await RespondAsync(components: components, ephemeral: true);
        }

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

        [SlashCommand("allowed", "View and manage users and roles that can manage Genshin Impact commands")]
        public async Task AllowedAsync()
        {
            var container = new ContainerBuilder();

            container.WithTextDisplay("## 🔐 Genshin Impact Permissions");
            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);

            if (AllowedUsers.Any())
            {
                var userList = string.Join("\n", AllowedUsers.Select(id => $"👤 <@{id}>"));
                container.WithTextDisplay($"### Allowed Users\n{userList}");
            }
            else
                container.WithTextDisplay("### Allowed Users\n-# No users currently allowed");

            container.WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small);

            if (AllowedRoles.Any())
            {
                var roleList = string.Join("\n", AllowedRoles.Select(id => $"🎭 <@&{id}>"));
                container.WithTextDisplay($"### Allowed Roles\n{roleList}");
            }
            else
                container.WithTextDisplay("### Allowed Roles\n-# No roles currently allowed");

            container.WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small);
            container.WithTextDisplay("-# Use the selectors below to add users or roles");

            container.WithActionRow(new ActionRowBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId("add_allowed_user")
                    .WithPlaceholder("Add allowed user...")
                    .WithMaxValues(1)
                    .WithType(ComponentType.UserSelect))
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId("add_allowed_role")
                    .WithPlaceholder("Add allowed role...")
                    .WithMaxValues(1)
                    .WithType(ComponentType.RoleSelect)));

            if (AllowedUsers.Any() || AllowedRoles.Any())
            {
                var removeRow = new ActionRowBuilder();
                if (AllowedUsers.Any())
                {
                    removeRow.WithSelectMenu(new SelectMenuBuilder()
                        .WithCustomId("remove_allowed_user")
                        .WithPlaceholder("Remove allowed user...")
                        .WithMaxValues(1)
                        .WithType(ComponentType.UserSelect));
                }
                if (AllowedRoles.Any())
                {
                    removeRow.WithSelectMenu(new SelectMenuBuilder()
                        .WithCustomId("remove_allowed_role")
                        .WithPlaceholder("Remove allowed role...")
                        .WithMaxValues(1)
                        .WithType(ComponentType.RoleSelect));
                }
                container.WithActionRow(removeRow);
            }

            container.WithAccentColor(VersionColor);
            var components = new ComponentBuilderV2().WithContainer(container).Build();

            await RespondAsync(components: components, ephemeral: true);
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
        {
            var components = new ComponentBuilderV2()
                .WithContainer(GetLivestreamContainer())
                .WithActionRow(GetActionRow())
                .Build();
            await RespondAsync(components: components, ephemeral: true);
        }

        [SlashCommand("channel-remove", "Stop tracking the Genshin Impact update in a specific channel")]
        [RequireAllowedUserInteraction]
        public async Task RemoveChannel(IGuildChannel channel)
        {
            var id = $"update:{channel.Guild.Id}";
            RecurringJob.RemoveIfExists(id);

            await RespondAsync($"Removed {MentionUtils.MentionChannel(channel.Id)} from the update tracking list.");
        }

        [SlashCommand("code-remove", "Remove a code from the Genshin Impact codes list")]
        [RequireAllowedUserInteraction]
        public async Task RemoveCode(string code)
        {
            Codes.TryRemove(code, out _);
            await ReplyCodesAsync(Context);
        }

        [SlashCommand("settings", "View and modify Genshin Impact settings")]
        [RequireAllowedUserInteraction]
        public async Task SettingsAsync()
        {
            var components = BuildSettingsComponents();
            await RespondAsync(components: components, ephemeral: true);
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

        [SlashCommand("versions", "View all pre-populated version schedules")]
        public async Task VersionsAsync()
        {
            var lines = AllVersions.Select((v, i) =>
            {
                var marker = i == CurrentVersionIndex ? "▶ " : "　";
                var lsTs = v.LivestreamStart.ToUnixTimeSeconds();
                var mtTs = v.MaintenanceStart.ToUnixTimeSeconds();
                return $"{marker}**{v.Name}** • Stream: <t:{lsTs}:d> • Patch: <t:{mtTs}:d>";
            });

            var container = new ContainerBuilder()
                .WithTextDisplay($"## 📅 Genshin Impact Version Schedule\n{string.Join("\n", lines)}")
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay("-# ▶ = active version • Confirmed through 6.4, projected through 7.0")
                .WithAccentColor(VersionColor);

            var components = new ComponentBuilderV2().WithContainer(container).Build();
            await RespondAsync(components: components, ephemeral: true);
        }

        [SlashCommand("advance", "Advance to the next version in the schedule")]
        [RequireAllowedUserInteraction]
        public async Task AdvanceAsync()
        {
            var next = GetNextVersion();
            if (next is null)
            {
                await RespondAsync("No more versions in the schedule.", ephemeral: true);
                return;
            }

            var prev = Version;
            LoadVersion(CurrentVersionIndex + 1);

            var lsTs = next.LivestreamStart.ToUnixTimeSeconds();
            var mtTs = next.MaintenanceStart.ToUnixTimeSeconds();
            var components = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay(
                        $"## ⏭ Advanced to {Version}\n" +
                        $"**From:** {prev}\n" +
                        $"**Stream:** <t:{lsTs}:F>\n" +
                        $"**Maintenance:** <t:{mtTs}:F>\n" +
                        $"-# Codes cleared. Set new image URLs with `/genshin set livestream-image` and `/genshin set maintenance-image`")
                    .WithAccentColor(VersionColor))
                .Build();
            await RespondAsync(components: components, ephemeral: true);
        }

        [SlashCommand("load", "Load a specific version from the schedule by index (0-based)")]
        [RequireAllowedUserInteraction]
        public async Task LoadAsync(int index)
        {
            if (index < 0 || index >= AllVersions.Count)
            {
                await RespondAsync($"Index must be between 0 and {AllVersions.Count - 1}.", ephemeral: true);
                return;
            }

            LoadVersion(index);
            var data = AllVersions[index];
            var lsTs = data.LivestreamStart.ToUnixTimeSeconds();
            var mtTs = data.MaintenanceStart.ToUnixTimeSeconds();
            var components = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay(
                        $"## 📦 Loaded {Version}\n" +
                        $"**Stream:** <t:{lsTs}:F>\n" +
                        $"**Maintenance:** <t:{mtTs}:F>")
                    .WithAccentColor(VersionColor))
                .Build();
            await RespondAsync(components: components, ephemeral: true);
        }

        [SlashCommand("export", "Export the version schedule as a JSON file")]
        [RequireAllowedUserInteraction]
        public async Task ExportAsync()
        {
            var json = ExportJson();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await RespondWithFileAsync(stream, "genshin_versions.json", "Exported version schedule.");
        }

        [SlashCommand("import", "Import version schedule from a JSON file or message URL")]
        [RequireAllowedUserInteraction]
        public async Task ImportAsync(IAttachment? file = null, string? message_url = null)
        {
            string? json = null;

            if (file is not null)
            {
                json = await Client.GetStringAsync(file.Url);
            }
            else if (message_url is not null)
            {
                var match = Regex.Match(message_url,
                    @"https?://(?:canary\.|ptb\.)?discord(?:app)?\.com/channels/(\d+)/(\d+)/(\d+)");
                if (!match.Success)
                {
                    await RespondAsync("Invalid message URL.", ephemeral: true);
                    return;
                }

                var guildId = ulong.Parse(match.Groups[1].Value);
                var channelId = ulong.Parse(match.Groups[2].Value);
                var messageId = ulong.Parse(match.Groups[3].Value);

                var guild = client.GetGuild(guildId);
                if (guild?.GetTextChannel(channelId) is { } ch)
                {
                    var msg = await ch.GetMessageAsync(messageId);
                    var attachment = msg?.Attachments.FirstOrDefault();
                    if (attachment is not null)
                        json = await Client.GetStringAsync(attachment.Url);
                    else if (msg?.Content is { Length: > 0 } content)
                        json = content;
                }
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                await RespondAsync("Provide a file upload or a message URL containing JSON.", ephemeral: true);
                return;
            }

            if (ImportJson(json))
            {
                var components = new ComponentBuilderV2()
                    .WithContainer(new ContainerBuilder()
                        .WithTextDisplay(
                            $"## ✅ Imported {AllVersions.Count} versions\n" +
                            $"**Active:** {Version} (#{CurrentVersionIndex})")
                        .WithAccentColor(VersionColor))
                    .Build();
                await RespondAsync(components: components, ephemeral: true);
            }
            else
                await RespondAsync("Failed to parse JSON.", ephemeral: true);
        }

        [ComponentInteraction("add_allowed_role", true)]
        [RequireAllowedUserInteraction]
        public async Task AddAllowedRoleAsync(IRole[] roles)
        {
            var role = roles.FirstOrDefault();
            if (role is null) return;

            if (AllowedRoles.Add(role.Id))
                await RespondAsync($"Added {role.Mention} to allowed roles.", ephemeral: true);
            else
                await RespondAsync($"{role.Mention} is already an allowed role.", ephemeral: true);
        }

        [ComponentInteraction("add_allowed_user", true)]
        [RequireAllowedUserInteraction]
        public async Task AddAllowedUserAsync(IUser[] users)
        {
            var user = users.FirstOrDefault();
            if (user is null) return;

            if (AllowedUsers.Add(user.Id))
                await RespondAsync($"Added {user.Mention} to allowed users.", ephemeral: true);
            else
                await RespondAsync($"{user.Mention} is already an allowed user.", ephemeral: true);
        }

        [ComponentInteraction("advanced_settings", true)]
        [RequireAllowedUserInteraction]
        public async Task AdvancedSettingsButton()
            => await Context.Interaction.RespondWithModalAsync<GenshinAdvancedSettingsModal>("advanced_settings_modal",
                modifyModal: m =>
                {
                    m.UpdateTextInput("maintenanceStart", x => x.Value    = Maintenance.Start.ToString("MM/dd/yyyy h:mm tt zzz"));
                    m.UpdateTextInput("maintenanceDuration", x => x.Value = MaintenanceDuration.ToString(@"h\:mm"));
                    m.UpdateTextInput("livestreamImage", x => x.Value     = LivestreamImage);
                    m.UpdateTextInput("maintenanceImage", x => x.Value    = MaintenanceImage);
                });

        [ComponentInteraction("codes", true)]
        public async Task CodesAsync() => await ReplyCodesAsync(Context);

        [ComponentInteraction("modify_settings", true)]
        [RequireAllowedUserInteraction]
        public async Task ModifySettingsButton()
            => await Context.Interaction.RespondWithModalAsync<GenshinSettingsModal>("settings_modal",
                modifyModal: m =>
                {
                    m.UpdateTextInput("version", x => x.Value            = Version);
                    m.UpdateTextInput("versionColor", x => x.Value       = VersionColor.ToString("X"));
                    m.UpdateTextInput("codeExpiry", x => x.Value         = CodeExpiry.ToString("MM/dd/yyyy h:mm tt zzz"));
                    m.UpdateTextInput("livestreamStart", x => x.Value    = Livestream.Start.ToString("MM/dd/yyyy h:mm tt zzz"));
                    m.UpdateTextInput("livestreamDuration", x => x.Value = LivestreamDuration.ToString(@"h\:mm"));
                });

        [ModalInteraction("advanced_settings_modal", true)]
        [RequireAllowedUserInteraction]
        public async Task OnAdvancedSettingsModalSubmit(GenshinAdvancedSettingsModal modal)
        {
            // Validate MaintenanceStart (e.g. "10/9/2024 6:00 AM +8")
            if (!DateTimeOffset.TryParse(modal.MaintenanceStart, out var parsedMaintenance))
            {
                await RespondAsync("Invalid Maintenance Start. Please follow the format e.g. 10/9/2024 6:00 AM +8",
                    ephemeral: true);
                return;
            }

            // Validate MaintenanceDuration (e.g. "5:00")
            if (!TimeSpan.TryParseExact(modal.MaintenanceDuration, @"h\:mm", null, out var parsedMaintenanceDuration))
            {
                await RespondAsync("Invalid Maintenance Duration. Please follow the format e.g. 5:00 (hours:minutes)",
                    ephemeral: true);
                return;
            }

            // Validate image URLs (basic URL validation)
            if (!string.IsNullOrWhiteSpace(modal.LivestreamImage) && !Uri.TryCreate(modal.LivestreamImage, UriKind.Absolute, out _))
            {
                await RespondAsync("Invalid Livestream Image URL. Please provide a valid URL.", ephemeral: true);
                return;
            }

            if (!string.IsNullOrWhiteSpace(modal.MaintenanceImage) && !Uri.TryCreate(modal.MaintenanceImage, UriKind.Absolute, out _))
            {
                await RespondAsync("Invalid Maintenance Image URL. Please provide a valid URL.", ephemeral: true);
                return;
            }

            // Update advanced settings
            MaintenanceDuration = parsedMaintenanceDuration;
            Maintenance         = Distance(parsedMaintenance.ToUnixTimeSeconds(), MaintenanceDuration);

            if (!string.IsNullOrWhiteSpace(modal.LivestreamImage))
                LivestreamImage = modal.LivestreamImage.Trim();
            if (!string.IsNullOrWhiteSpace(modal.MaintenanceImage))
                MaintenanceImage = modal.MaintenanceImage.Trim();

            await RespondAsync("All settings updated successfully! ✨", ephemeral: true);
        }

        [ModalInteraction("settings_modal", true)]
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

            // Validate LivestreamDuration (e.g. "1:30")
            if (!TimeSpan.TryParseExact(modal.LivestreamDuration, @"h\:mm", null, out var parsedLivestreamDuration))
            {
                await RespondAsync("Invalid Livestream Duration. Please follow the format e.g. 1:30 (hours:minutes)",
                    ephemeral: true);
                return;
            }

            // Update basic settings
            Version            = modal.Version.Trim();
            VersionColor       = parsedColor;
            CodeExpiry         = parsedExpiry;
            LivestreamDuration = parsedLivestreamDuration;

            // Update livestream range with new duration
            Livestream = Distance(parsedLivestream.ToUnixTimeSeconds(), LivestreamDuration);

            // Show completion message with option to configure advanced settings
            var components = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay(
                        "## ✅ Basic settings updated\n" +
                        "You can now configure advanced settings (maintenance & images) if needed.")
                    .WithAccentColor(VersionColor))
                .WithActionRow(new ActionRowBuilder()
                    .WithButton(new ButtonBuilder("Configure Advanced Settings", "advanced_settings",
                        ButtonStyle.Secondary)))
                .Build();

            await RespondAsync(components: components, ephemeral: true);
        }

        [ComponentInteraction("remove_allowed_role", true)]
        [RequireAllowedUserInteraction]
        public async Task RemoveAllowedRoleAsync(IRole[] roles)
        {
            var role = roles.FirstOrDefault();
            if (role is null) return;

            if (AllowedRoles.Remove(role.Id))
                await RespondAsync($"Removed {role.Mention} from allowed roles.", ephemeral: true);
            else
                await RespondAsync($"{role.Mention} was not an allowed role.", ephemeral: true);
        }

        [ComponentInteraction("remove_allowed_user", true)]
        [RequireAllowedUserInteraction]
        public async Task RemoveAllowedUserAsync(IUser[] users)
        {
            var user = users.FirstOrDefault();
            if (user is null) return;

            if (AllowedUsers.Remove(user.Id))
                await RespondAsync($"Removed {user.Mention} from allowed users.", ephemeral: true);
            else
                await RespondAsync($"{user.Mention} was not an allowed user.", ephemeral: true);
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

        [Discord.Interactions.Group("set", "Modify Genshin Impact settings individually")]
        public class GenshinSetCommands : InteractionModuleBase<SocketInteractionContext>
        {
            [SlashCommand("version", "Set the version name")]
            [RequireAllowedUserInteraction]
            public async Task SetVersion(string version, int target = -1)
            {
                var (idx, v) = ResolveTarget(target);
                if (v is null) { await InvalidTarget(); return; }
                v.Name = version.Trim();
                if (idx == CurrentVersionIndex) SyncToActive();
                await RespondSettingChanged(idx, "Version", v.Name);
            }

            [SlashCommand("color", "Set the version accent color (hex)")]
            [RequireAllowedUserInteraction]
            public async Task SetColor(string hex, int target = -1)
            {
                var input = hex.Trim().TrimStart('#');
                if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) input = input[2..];
                if (!uint.TryParse(input, NumberStyles.HexNumber, null, out var color))
                { await RespondAsync("Invalid hex color. Use `0xfeef5f`, `#feef5f`, or `feef5f`.", ephemeral: true); return; }

                var (idx, v) = ResolveTarget(target);
                if (v is null) { await InvalidTarget(); return; }
                v.Color = color;
                if (idx == CurrentVersionIndex) SyncToActive();
                await RespondSettingChanged(idx, "Color", $"0x{v.Color:X}");
            }

            [SlashCommand("code-expiry", "Set the code expiry date")]
            [RequireAllowedUserInteraction]
            public async Task SetCodeExpiry(string datetime, int target = -1)
            {
                if (!DateTimeOffset.TryParse(datetime, out var parsed))
                { await RespondAsync("Invalid date. Format: `9/28/2024 12:00 PM +8`", ephemeral: true); return; }

                var (idx, v) = ResolveTarget(target);
                if (v is null) { await InvalidTarget(); return; }
                v.CodeExpiry = parsed;
                if (idx == CurrentVersionIndex) SyncToActive();
                await RespondSettingChanged(idx, "Code Expiry", $"<t:{parsed.ToUnixTimeSeconds()}:F>");
            }

            [SlashCommand("livestream-start", "Set the livestream start time")]
            [RequireAllowedUserInteraction]
            public async Task SetLivestreamStart(string datetime, int target = -1)
            {
                if (!DateTimeOffset.TryParse(datetime, out var parsed))
                { await RespondAsync("Invalid date. Format: `9/27/2024 8:00 PM +8`", ephemeral: true); return; }

                var (idx, v) = ResolveTarget(target);
                if (v is null) { await InvalidTarget(); return; }
                v.LivestreamStart = parsed;
                if (idx == CurrentVersionIndex) SyncToActive();
                await RespondSettingChanged(idx, "Livestream Start", $"<t:{parsed.ToUnixTimeSeconds()}:F>");
            }

            [SlashCommand("livestream-duration", "Set the livestream duration (h:mm)")]
            [RequireAllowedUserInteraction]
            public async Task SetLivestreamDuration(string duration, int target = -1)
            {
                if (!TimeSpan.TryParseExact(duration.Trim(), @"h\:mm", null, out var parsed))
                { await RespondAsync("Invalid duration. Format: `1:30` (hours:minutes)", ephemeral: true); return; }

                var (idx, v) = ResolveTarget(target);
                if (v is null) { await InvalidTarget(); return; }
                v.LivestreamDuration = parsed;
                if (idx == CurrentVersionIndex) SyncToActive();
                await RespondSettingChanged(idx, "Livestream Duration", parsed.Humanize());
            }

            [SlashCommand("livestream-image", "Set the livestream image URL")]
            [RequireAllowedUserInteraction]
            public async Task SetLivestreamImage(string url, int target = -1)
            {
                if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
                { await RespondAsync("Invalid URL.", ephemeral: true); return; }

                var (idx, v) = ResolveTarget(target);
                if (v is null) { await InvalidTarget(); return; }
                v.LivestreamImage = url.Trim();
                if (idx == CurrentVersionIndex) SyncToActive();
                await RespondSettingChanged(idx, "Livestream Image", v.LivestreamImage);
            }

            [SlashCommand("maintenance-start", "Set the maintenance start time")]
            [RequireAllowedUserInteraction]
            public async Task SetMaintenanceStart(string datetime, int target = -1)
            {
                if (!DateTimeOffset.TryParse(datetime, out var parsed))
                { await RespondAsync("Invalid date. Format: `10/9/2024 6:00 AM +8`", ephemeral: true); return; }

                var (idx, v) = ResolveTarget(target);
                if (v is null) { await InvalidTarget(); return; }
                v.MaintenanceStart = parsed;
                if (idx == CurrentVersionIndex) SyncToActive();
                await RespondSettingChanged(idx, "Maintenance Start", $"<t:{parsed.ToUnixTimeSeconds()}:F>");
            }

            [SlashCommand("maintenance-duration", "Set the maintenance duration (h:mm)")]
            [RequireAllowedUserInteraction]
            public async Task SetMaintenanceDuration(string duration, int target = -1)
            {
                if (!TimeSpan.TryParseExact(duration.Trim(), @"h\:mm", null, out var parsed))
                { await RespondAsync("Invalid duration. Format: `5:00` (hours:minutes)", ephemeral: true); return; }

                var (idx, v) = ResolveTarget(target);
                if (v is null) { await InvalidTarget(); return; }
                v.MaintenanceDuration = parsed;
                if (idx == CurrentVersionIndex) SyncToActive();
                await RespondSettingChanged(idx, "Maintenance Duration", parsed.Humanize());
            }

            [SlashCommand("maintenance-image", "Set the maintenance image URL")]
            [RequireAllowedUserInteraction]
            public async Task SetMaintenanceImage(string url, int target = -1)
            {
                if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
                { await RespondAsync("Invalid URL.", ephemeral: true); return; }

                var (idx, v) = ResolveTarget(target);
                if (v is null) { await InvalidTarget(); return; }
                v.MaintenanceImage = url.Trim();
                if (idx == CurrentVersionIndex) SyncToActive();
                await RespondSettingChanged(idx, "Maintenance Image", v.MaintenanceImage);
            }

            private Task InvalidTarget() =>
                RespondAsync($"Index must be 0–{AllVersions.Count - 1}.", ephemeral: true);

            private async Task RespondSettingChanged(int idx, string setting, string value)
            {
                var v = AllVersions[idx];
                var label = idx == CurrentVersionIndex ? $"{v.Name} (active)" : $"{v.Name} (#{idx})";
                var components = new ComponentBuilderV2()
                    .WithContainer(new ContainerBuilder()
                        .WithTextDisplay($"### ✅ {setting}\n**{label}** → {value}")
                        .WithAccentColor(v.Color))
                    .Build();
                await RespondAsync(components: components, ephemeral: true);
            }
        }
    }

    public class GenshinCommands : ModuleBase<SocketCommandContext>
    {
        [Command("code add")]
        [Alias("codes add")]
        [RequireAllowedUserCommand]
        [Summary("Adds a code to the Genshin Impact codes list")]
        public async Task AddCode(string code, int gems)
        {
            Codes.AddOrUpdate(code, gems, (_, _) => gems);
            await ReplyCodesAsync(Context);
        }

        [Command("genshin allow")]
        [RequireAllowedUserCommand]
        [Summary("Allows a role to manage the Genshin Impact commands")]
        public async Task AllowRole(IRole role)
        {
            if (AllowedRoles.Add(role.Id))
                await ReplyGenshinAsync("Genshin Permissions", $"Added {role.Mention} to allowed roles.");
            else
                await ReplyGenshinAsync("Genshin Permissions", $"{role.Mention} is already an allowed role.");
        }

        [Command("genshin allow")]
        [RequireAllowedUserCommand]
        [Summary("Allows a user or role to manage the Genshin Impact commands")]
        public async Task AllowUser(IGuildUser user)
        {
            if (AllowedUsers.Add(user.Id))
                await ReplyGenshinAsync("Genshin Permissions", $"Added {user.Mention} to allowed users.");
            else
                await ReplyGenshinAsync("Genshin Permissions", $"{user.Mention} is already an allowed user.");
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

        [Command("genshin disallow")]
        [RequireAllowedUserCommand]
        [Summary("Disallows a role from managing the Genshin Impact commands")]
        public async Task DisallowRole(IRole role)
        {
            if (AllowedRoles.Remove(role.Id))
                await ReplyGenshinAsync("Genshin Permissions", $"Removed {role.Mention} from allowed roles.");
            else
                await ReplyGenshinAsync("Genshin Permissions", $"{role.Mention} was not an allowed role.");
        }

        [Command("genshin disallow")]
        [RequireAllowedUserCommand]
        [Summary("Disallows a user from managing the Genshin Impact commands")]
        public async Task DisallowUser(IGuildUser user)
        {
            if (AllowedUsers.Remove(user.Id))
                await ReplyGenshinAsync("Genshin Permissions", $"Removed {user.Mention} from allowed users.");
            else
                await ReplyGenshinAsync("Genshin Permissions", $"{user.Mention} was not an allowed user.");
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

            await ReplyAsync(
                components: embed.Build().ToComponentsV2Message(),
                allowedMentions: AllowedMentions.None);
        }

        [Command("livestream")]
        [Alias("live", "stream", "update", "patch")]
        [Summary("View the remaining time for Genshin's {Version} Update")]
        public async Task LivestreamAsync()
        {
            var components = new ComponentBuilderV2()
                .WithContainer(GetLivestreamContainer())
                .WithActionRow(GetActionRow())
                .Build();
            await ReplyAsync(components: components);
        }

        [Command("code remove")]
        [Alias("codes remove")]
        [RequireAllowedUserCommand]
        [Summary("Removes a code from the Genshin Impact codes list")]
        public async Task RemoveCode(string code)
        {
            if (Codes.TryRemove(code, out _))
                await ReplyCodesAsync(Context);
            else
                await ReplyGenshinAsync("Genshin Codes", "Code not found.");
        }

        [Command("genshin settings")]
        [RequireAllowedUserCommand]
        [Summary("View and modify Genshin Impact settings")]
        public async Task SettingsAsync()
        {
            var components = BuildSettingsComponents();
            await ReplyAsync(components: components);
        }

        [Command("code clear")]
        [Alias("codes clear")]
        [RequireAllowedUserCommand]
        [Summary("Clears all codes from the list")]
        public async Task ClearCodes()
        {
            Codes.Clear();
            await ReplyGenshinAsync("Genshin Codes", "All codes have been cleared.");
        }

        [Command("genshin set")]
        [RequireAllowedUserCommand]
        [Summary("Set a setting on the active version")]
        public async Task SetSetting(string setting, [Remainder] string value)
            => await ApplySetSetting(CurrentVersionIndex, setting, value);

        [Command("genshin set-at")]
        [RequireAllowedUserCommand]
        [Summary("Set a setting on a specific version: set-at <index> <setting> <value>")]
        public async Task SetSettingAt(int index, string setting, [Remainder] string value)
            => await ApplySetSetting(index, setting, value);

        [Command("genshin export")]
        [RequireAllowedUserCommand]
        [Summary("Export the version schedule as a JSON file")]
        public async Task ExportCommand()
        {
            var json = ExportJson();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await Context.Channel.SendFileAsync(stream, "genshin_versions.json", "Exported version schedule.");
        }

        [Command("genshin import")]
        [RequireAllowedUserCommand]
        [Summary("Import version schedule from an attached JSON file or message URL")]
        public async Task ImportCommand([Remainder] string? messageUrl = null)
        {
            string? json = null;

            var attachment = Context.Message.Attachments.FirstOrDefault();
            if (attachment is not null)
            {
                json = await Client.GetStringAsync(attachment.Url);
            }
            else if (messageUrl is not null)
            {
                var match = Regex.Match(messageUrl,
                    @"https?://(?:canary\.|ptb\.)?discord(?:app)?\.com/channels/(\d+)/(\d+)/(\d+)");
                if (match.Success)
                {
                    var channelId = ulong.Parse(match.Groups[2].Value);
                    var messageId = ulong.Parse(match.Groups[3].Value);
                    if (Context.Client.GetChannel(channelId) is ITextChannel ch)
                    {
                        var msg = await ch.GetMessageAsync(messageId);
                        var att = msg?.Attachments.FirstOrDefault();
                        if (att is not null)
                            json = await Client.GetStringAsync(att.Url);
                        else if (msg?.Content is { Length: > 0 } content)
                            json = content;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                await ReplyGenshinAsync("Import", "Attach a JSON file or provide a message URL.");
                return;
            }

            if (ImportJson(json))
                await ReplyGenshinAsync("Import",
                    $"Imported **{AllVersions.Count}** versions. Active: **{Version}** (#{CurrentVersionIndex})");
            else
                await ReplyGenshinAsync("Import", "Failed to parse JSON.");
        }

        private async Task ApplySetSetting(int index, string setting, string value)
        {
            if (index < 0 || index >= AllVersions.Count)
            { await ReplyGenshinAsync("Settings", $"Index must be 0–{AllVersions.Count - 1}."); return; }

            var v = AllVersions[index];
            var label = index == CurrentVersionIndex ? $"{v.Name} (active)" : $"{v.Name} (#{index})";

            switch (setting.ToLowerInvariant())
            {
                case "version":
                    v.Name = value.Trim();
                    if (index == CurrentVersionIndex) SyncToActive();
                    await ReplyGenshinAsync("Settings", $"**{label}** Version → {v.Name}");
                    break;

                case "color":
                    var ci = value.Trim().TrimStart('#');
                    if (ci.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) ci = ci[2..];
                    if (!uint.TryParse(ci, NumberStyles.HexNumber, null, out var color))
                    { await ReplyGenshinAsync("Settings", "Invalid hex color."); return; }
                    v.Color = color;
                    if (index == CurrentVersionIndex) SyncToActive();
                    await ReplyGenshinAsync("Settings", $"**{label}** Color → 0x{v.Color:X}");
                    break;

                case "expiry" or "code-expiry":
                    if (!DateTimeOffset.TryParse(value, out var expiry))
                    { await ReplyGenshinAsync("Settings", "Invalid date."); return; }
                    v.CodeExpiry = expiry;
                    if (index == CurrentVersionIndex) SyncToActive();
                    await ReplyGenshinAsync("Settings", $"**{label}** Code Expiry → <t:{expiry.ToUnixTimeSeconds()}:F>");
                    break;

                case "livestream-start":
                    if (!DateTimeOffset.TryParse(value, out var ls))
                    { await ReplyGenshinAsync("Settings", "Invalid date."); return; }
                    v.LivestreamStart = ls;
                    if (index == CurrentVersionIndex) SyncToActive();
                    await ReplyGenshinAsync("Settings", $"**{label}** Stream Start → <t:{ls.ToUnixTimeSeconds()}:F>");
                    break;

                case "livestream-duration":
                    if (!TimeSpan.TryParseExact(value.Trim(), @"h\:mm", null, out var ld))
                    { await ReplyGenshinAsync("Settings", "Invalid duration. Format: `1:30`"); return; }
                    v.LivestreamDuration = ld;
                    if (index == CurrentVersionIndex) SyncToActive();
                    await ReplyGenshinAsync("Settings", $"**{label}** Stream Duration → {ld.Humanize()}");
                    break;

                case "livestream-image":
                    if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out _))
                    { await ReplyGenshinAsync("Settings", "Invalid URL."); return; }
                    v.LivestreamImage = value.Trim();
                    if (index == CurrentVersionIndex) SyncToActive();
                    await ReplyGenshinAsync("Settings", $"**{label}** Stream Image updated.");
                    break;

                case "maintenance-start":
                    if (!DateTimeOffset.TryParse(value, out var ms))
                    { await ReplyGenshinAsync("Settings", "Invalid date."); return; }
                    v.MaintenanceStart = ms;
                    if (index == CurrentVersionIndex) SyncToActive();
                    await ReplyGenshinAsync("Settings", $"**{label}** Maint Start → <t:{ms.ToUnixTimeSeconds()}:F>");
                    break;

                case "maintenance-duration":
                    if (!TimeSpan.TryParseExact(value.Trim(), @"h\:mm", null, out var md))
                    { await ReplyGenshinAsync("Settings", "Invalid duration. Format: `5:00`"); return; }
                    v.MaintenanceDuration = md;
                    if (index == CurrentVersionIndex) SyncToActive();
                    await ReplyGenshinAsync("Settings", $"**{label}** Maint Duration → {md.Humanize()}");
                    break;

                case "maintenance-image":
                    if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out _))
                    { await ReplyGenshinAsync("Settings", "Invalid URL."); return; }
                    v.MaintenanceImage = value.Trim();
                    if (index == CurrentVersionIndex) SyncToActive();
                    await ReplyGenshinAsync("Settings", $"**{label}** Maint Image updated.");
                    break;

                default:
                    await ReplyGenshinAsync("Settings",
                        "Settings: `version`, `color`, `expiry`, `livestream-start`, `livestream-duration`, " +
                        "`livestream-image`, `maintenance-start`, `maintenance-duration`, `maintenance-image`");
                    break;
            }
        }

        [Command("genshin channel add")]
        [RequireAllowedUserCommand]
        [Summary("Track the Genshin Impact update in a channel")]
        public async Task TrackUpdateCommand(IGuildChannel channel)
        {
            var id = $"update:{channel.Guild.Id}";
            RecurringJob.AddOrUpdate<GenshinInteractive>(id,
                x => x.UpdatePatchAsync(channel.Guild.Id, channel.Id),
                "*/5 * * * *");

            await ReplyGenshinAsync("Channel Tracking",
                $"Added {MentionUtils.MentionChannel(channel.Id)} to the update tracking list.");
        }

        [Command("genshin channel remove")]
        [RequireAllowedUserCommand]
        [Summary("Stop tracking the Genshin Impact update in a channel")]
        public async Task RemoveChannelCommand(IGuildChannel channel)
        {
            var id = $"update:{channel.Guild.Id}";
            RecurringJob.RemoveIfExists(id);

            await ReplyGenshinAsync("Channel Tracking",
                $"Removed {MentionUtils.MentionChannel(channel.Id)} from the update tracking list.");
        }

        [Command("genshin channel list")]
        [RequireAllowedUserCommand]
        [Summary("View all channels tracking the Genshin Impact update")]
        public async Task ListChannelsCommand()
        {
            var allJobs = JobStorage.Current.GetConnection().GetRecurringJobs();
            var jobs = allJobs.Where(j => j.Id.StartsWith("update:"));
            var channels = jobs.Select(j => ulong.Parse(j.Id.Split(':')[1]));

            await ReplyGenshinAsync("Channel Tracking", channels.Humanize(MentionUtils.MentionChannel));
        }

        [Command("genshin versions")]
        [Summary("View all pre-populated version schedules")]
        public async Task VersionsCommand()
        {
            var lines = AllVersions.Select((v, i) =>
            {
                var marker = i == CurrentVersionIndex ? "▶ " : "　";
                var lsTs = v.LivestreamStart.ToUnixTimeSeconds();
                var mtTs = v.MaintenanceStart.ToUnixTimeSeconds();
                return $"{marker}**{v.Name}** • Stream: <t:{lsTs}:d> • Patch: <t:{mtTs}:d>";
            });

            await ReplyGenshinAsync("Version Schedule",
                $"{string.Join("\n", lines)}\n-# ▶ = active version • Confirmed through 6.4, projected through 7.0");
        }

        [Command("genshin advance")]
        [RequireAllowedUserCommand]
        [Summary("Advance to the next version in the schedule")]
        public async Task AdvanceCommand()
        {
            var next = GetNextVersion();
            if (next is null)
            {
                await ReplyGenshinAsync("Version Schedule", "No more versions in the schedule.");
                return;
            }

            var prev = Version;
            LoadVersion(CurrentVersionIndex + 1);
            var lsTs = next.LivestreamStart.ToUnixTimeSeconds();
            var mtTs = next.MaintenanceStart.ToUnixTimeSeconds();
            await ReplyGenshinAsync("Version Schedule",
                $"Advanced **{prev}** → **{Version}**\n" +
                $"**Stream:** <t:{lsTs}:F>\n" +
                $"**Maintenance:** <t:{mtTs}:F>\n" +
                $"-# Codes cleared. Set images with `genshin set livestream-image` and `genshin set maintenance-image`");
        }

        [Command("genshin load")]
        [RequireAllowedUserCommand]
        [Summary("Load a specific version from the schedule by index (0-based)")]
        public async Task LoadCommand(int index)
        {
            if (index < 0 || index >= AllVersions.Count)
            {
                await ReplyGenshinAsync("Version Schedule",
                    $"Index must be between 0 and {AllVersions.Count - 1}.");
                return;
            }

            LoadVersion(index);
            var data = AllVersions[index];
            var lsTs = data.LivestreamStart.ToUnixTimeSeconds();
            var mtTs = data.MaintenanceStart.ToUnixTimeSeconds();
            await ReplyGenshinAsync("Version Schedule",
                $"Loaded **{Version}**\n**Stream:** <t:{lsTs}:F>\n**Maintenance:** <t:{mtTs}:F>");
        }

        private async Task ReplyGenshinAsync(string title, string body)
        {
            var components = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay($"## {title}\n{body}")
                    .WithAccentColor(VersionColor))
                .Build();

            await ReplyAsync(components: components, allowedMentions: AllowedMentions.None);
        }
    }

    private class RequireAllowedUserCommandAttribute : CPreconditionAttribute
    {
        public override async Task<CPreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            if (context.User is not IGuildUser user)
                return CPreconditionResult.FromError("This command can only be used in a server.");

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
                return IPreconditionResult.FromError("This command can only be used in a server.");

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
        [InputLabel("Code Expiry")]
        [ModalTextInput("codeExpiry", TextInputStyle.Short, "e.g. 9/28/2024 12:00 PM +8", maxLength: 40)]
        public string CodeExpiry { get; set; } = null!;

        [InputLabel("Livestream Duration (h:mm)")]
        [ModalTextInput("livestreamDuration", TextInputStyle.Short, "e.g. 1:30", maxLength: 10)]
        public string LivestreamDuration { get; set; } = null!;

        [InputLabel("Livestream Start")]
        [ModalTextInput("livestreamStart", TextInputStyle.Short, "e.g. 9/27/2024 8:00 PM +8", maxLength: 40)]
        public string LivestreamStart { get; set; } = null!;

        [InputLabel("Version")]
        [ModalTextInput("version", TextInputStyle.Short, "e.g. 5.1", maxLength: 50)]
        public string Version { get; set; } = null!;

        [InputLabel("Version Color (Hex)")]
        [ModalTextInput("versionColor", TextInputStyle.Short, "e.g. 0xfeef5f", maxLength: 10)]
        public string VersionColor { get; set; } = null!;

        // Setting the CustomId to be matched by the ModalInteraction handler.
        public string Title => "Modify Genshin Settings (1/2)";
    }

    public class GenshinAdvancedSettingsModal : IModal
    {
        [InputLabel("Livestream Image URL")]
        [ModalTextInput("livestreamImage", TextInputStyle.Paragraph, "https://...", maxLength: 500)]
        public string LivestreamImage { get; set; } = null!;

        [InputLabel("Maintenance Duration (h:mm)")]
        [ModalTextInput("maintenanceDuration", TextInputStyle.Short, "e.g. 5:00", maxLength: 10)]
        public string MaintenanceDuration { get; set; } = null!;

        [InputLabel("Maintenance Image URL")]
        [ModalTextInput("maintenanceImage", TextInputStyle.Paragraph, "https://...", maxLength: 500)]
        public string MaintenanceImage { get; set; } = null!;

        [InputLabel("Maintenance Start (with timezone)")]
        [ModalTextInput("maintenanceStart", TextInputStyle.Short, "e.g. 10/9/2024 6:00 AM +8", maxLength: 40)]
        public string MaintenanceStart { get; set; } = null!;

        public string Title => "Modify Genshin Settings (2/2)";
    }
}