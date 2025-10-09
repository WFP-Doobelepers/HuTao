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
        784295266651471897, // Millelith
        966392349469069362,
        830577183730434118
    ];

    public static HashSet<ulong> AllowedUsers { get; } =
    [
        852717789071278100 // hime.san
    ];

    public static DateRange Distance(long start, TimeSpan duration) => new(start, start + (long) duration.TotalSeconds);

    public static async Task ReplyCodesAsync(Context context, ITextChannel? send = null, IUserMessage? update = null)
    {
        if (Codes.IsEmpty)
        {
            await context.ReplyAsync("There are no codes available yet.", ephemeral: true);
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

        var codesList = string.Join("\n", Codes.Select(c =>
            $"💎 **{c.Value}** primogems • **[{c.Key}](https://genshin.hoyoverse.com/en/gift?code={c.Key})**"));

        container.WithTextDisplay(codesList);
        container.WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small);
        container.WithTextDisplay("-# Click the buttons below to redeem codes directly");

        // Build action rows for buttons (outside container)
        var actionRow = new ActionRowBuilder()
            .WithButton("Redeem ⇾", "codes", ButtonStyle.Success, disabled: true);

        var codeButtons = Codes.Take(4).Select(code =>
            ButtonBuilder.CreateLinkButton(
                $"💎 {code.Value} {code.Key}",
                $"https://genshin.hoyoverse.com/en/gift?code={code.Key}"));

        foreach (var button in codeButtons)
        {
            actionRow.WithButton(button);
        }

        ActionRowBuilder? secondRow = null;
        if (Codes.Count > 4)
        {
            var remainingCodes = Codes.Skip(4).Take(4);
            secondRow = new ActionRowBuilder();
            foreach (var code in remainingCodes)
            {
                secondRow.WithButton(ButtonBuilder.CreateLinkButton(
                    $"💎 {code.Value} {code.Key}",
                    $"https://genshin.hoyoverse.com/en/gift?code={code.Key}"));
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

        [ComponentInteraction("add_allowed_role", true)]
        [RequireAllowedUserInteraction]
        public async Task AddAllowedRoleAsync(IRole[] roles)
        {
            var role = roles.FirstOrDefault();
            if (role is null) return;

            if (AllowedRoles.Add(role.Id))
                await RespondAsync($"✅ Added {role.Mention} to allowed roles.", ephemeral: true);
            else
                await RespondAsync($"⚠️ {role.Mention} is already an allowed role.", ephemeral: true);
        }

        [ComponentInteraction("add_allowed_user", true)]
        [RequireAllowedUserInteraction]
        public async Task AddAllowedUserAsync(IUser[] users)
        {
            var user = users.FirstOrDefault();
            if (user is null) return;

            if (AllowedUsers.Add(user.Id))
                await RespondAsync($"✅ Added {user.Mention} to allowed users.", ephemeral: true);
            else
                await RespondAsync($"⚠️ {user.Mention} is already an allowed user.", ephemeral: true);
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
            var components = new ComponentBuilder()
                .WithButton("Configure Advanced Settings", "advanced_settings", ButtonStyle.Secondary);

            await RespondAsync("Basic settings updated successfully! You can now configure advanced settings (maintenance & images) if needed.",
                components: components.Build(), ephemeral: true);
        }

        [ComponentInteraction("remove_allowed_role", true)]
        [RequireAllowedUserInteraction]
        public async Task RemoveAllowedRoleAsync(IRole[] roles)
        {
            var role = roles.FirstOrDefault();
            if (role is null) return;

            if (AllowedRoles.Remove(role.Id))
                await RespondAsync($"🗑️ Removed {role.Mention} from allowed roles.", ephemeral: true);
            else
                await RespondAsync($"⚠️ {role.Mention} was not an allowed role.", ephemeral: true);
        }

        [ComponentInteraction("remove_allowed_user", true)]
        [RequireAllowedUserInteraction]
        public async Task RemoveAllowedUserAsync(IUser[] users)
        {
            var user = users.FirstOrDefault();
            if (user is null) return;

            if (AllowedUsers.Remove(user.Id))
                await RespondAsync($"🗑️ Removed {user.Mention} from allowed users.", ephemeral: true);
            else
                await RespondAsync($"⚠️ {user.Mention} was not an allowed user.", ephemeral: true);
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
                await ReplyAsync($"Added {role.Mention} to allowed roles.");
            else
                await ReplyAsync($"{role.Mention} is already an allowed role.");
        }

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
                await ReplyAsync($"Removed {role.Mention} from allowed roles.");
            else
                await ReplyAsync($"{role.Mention} was not an allowed role.");
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
        public Task RemoveCode(string code) => Codes.TryRemove(code, out _)
            ? ReplyCodesAsync(Context)
            : ReplyAsync("Code not found.");

        [Command("genshin settings")]
        [RequireAllowedUserCommand]
        [Summary("View and modify Genshin Impact settings")]
        public async Task SettingsAsync()
        {
            var components = BuildSettingsComponents();
            await ReplyAsync(components: components);
        }
    }

    private class RequireAllowedUserCommandAttribute : CPreconditionAttribute
    {
        public override async Task<CPreconditionResult> CheckPermissionsAsync(
            ICommandContext context,
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