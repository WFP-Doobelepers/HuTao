using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace HuTao.Bot.Modules;

[Group("import")]
[RequireAuthorization(AuthorizationScope.Configuration)]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
public class ImportModule : ModuleBase<SocketCommandContext>
{
    private const long Gaius = 356878329602768897;
    private const long Dyno = 155149108183695360;
    private const ulong Carl = 235148962103951360;

    private static readonly JsonSerializerSettings CarlSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        Formatting = Formatting.Indented
    };

    private readonly HttpClient _client;
    private readonly HuTaoContext _db;

    public ImportModule(HttpClient client, HuTaoContext db)
    {
        _client = client;
        _db     = db;
    }

    [Command("carl")]
    [Summary("Import Carl warnings into the database.")]
    public async Task ImportCarlHistoryAsync(string url, ModerationCategory? category = null)
    {
        Log.Information("Importing warnings from {Link} into {Category}", url, category?.Name);
        var stream = await _client.GetStringAsync(url);

        var history = JsonConvert.DeserializeObject<List<CarlLog>>(stream, CarlSettings);
        if (history?.Any() != true)
        {
            await ReplyAsync("No warnings found.");
            return;
        }

        Log.Verbose("Found {Count} warnings", history.Count);
        var trackedUsers = new HashSet<ulong>();
        var mutes = new Dictionary<ulong, ExpirableReprimand>();
        var bans = new Dictionary<ulong, ExpirableReprimand>();
        var failed = new List<CarlLog>();

        foreach (var detail in history.OrderBy(h => h.CaseId))
        {
            Log.Information("Importing warning {CaseId}", detail.CaseId);
            var moderator = detail.ModeratorId;
            if (moderator is null)
                Log.Warning("Warning {CaseId} has no moderator", detail.CaseId);

            if (!trackedUsers.Contains(detail.OffenderId))
            {
                Log.Verbose("Getting user {User}", detail.OffenderId);
                await _db.Users.TrackUserAsync(detail.OffenderId, Context.Guild.Id);

                trackedUsers.Add(detail.OffenderId);
            }
            if (moderator is not null && !trackedUsers.Contains(moderator.Value))
            {
                Log.Verbose("Getting moderator {Mod}", moderator);
                await _db.Users.TrackUserAsync(moderator.Value, Context.Guild.Id);

                trackedUsers.Add(moderator.Value);
            }

            Log.Verbose("Creating infraction {User}", detail.OffenderId);

            // If the action is a removal of a reprimand, we must find the previous infraction
            ExpirableReprimand? previous = null;
            if (detail.Action is CarlAction.Unban or CarlAction.Unmute)
            {
                Log.Information("Finding previous infraction {User}", detail.OffenderId);
                _ = detail.Action switch
                {
                    CarlAction.Unban  => bans.Remove(detail.OffenderId, out previous),
                    CarlAction.Unmute => mutes.Remove(detail.OffenderId, out previous),
                    _                 => default
                };
            }

            var details = new ReprimandShort(detail.OffenderId, moderator ?? Carl,
                Context.Guild.Id, detail.Reason, Category: category);

            var action = moderator is null
                ? null
                : new ModerationAction(new ActionDetails(moderator.Value, Context.Guild.Id, details.Reason))
                {
                    Date = detail.Timestamp.ToUniversalTime()
                };

            if (previous is not null)
            {
                Log.Verbose("Updating previous infraction {User}", detail.OffenderId);
                previous.Status         = moderator == Carl ? ReprimandStatus.Expired : ReprimandStatus.Pardoned;
                previous.EndedAt        = detail.Timestamp.ToUniversalTime();
                previous.Length         = previous.EndedAt - previous.StartedAt;
                previous.ModifiedAction = action;
            }
            else
            {
                Reprimand? reprimand = detail.Action switch
                {
                    CarlAction.Warn => new Warning(1, null, details)
                    {
                        StartedAt = detail.Timestamp.ToUniversalTime(),
                        Action    = action
                    },
                    CarlAction.Mute => new Mute(null, details)
                    {
                        StartedAt = detail.Timestamp.ToUniversalTime(),
                        Action    = action
                    },
                    CarlAction.Ban or CarlAction.TempBan => new Ban(0, null, details)
                    {
                        StartedAt = detail.Timestamp.ToUniversalTime(),
                        Action    = action
                    },
                    CarlAction.Kick => new Kick(details)
                    {
                        Action = action
                    },
                    _ => null
                };

                if (reprimand is null)
                {
                    Log.Warning("Failed to create infraction for case {CaseId}", detail.CaseId);
                    failed.Add(detail);
                    continue;
                }

                Log.Verbose("Added infraction of type {Type}", reprimand.GetType().Name);
                _db.Add(reprimand);
                _ = reprimand switch
                {
                    Ban b  => bans[detail.OffenderId]  = b,
                    Mute m => mutes[detail.OffenderId] = m,
                    _      => null
                };
            }
        }

        // Expire mutes that are still in the mutes list
        var current = Context.Guild.CurrentUser;
        var muteAction = new ModerationAction(new ActionDetails(current.Id, Context.Guild.Id, "[Reprimand Expired]"));
        foreach (var (user, mute) in mutes)
        {
            Log.Verbose("Expiring mute {User} which had no unmute entry", user);
            mute.Status         = ReprimandStatus.Expired;
            mute.EndedAt        = DateTime.UtcNow;
            mute.Length         = mute.EndedAt - mute.StartedAt;
            mute.ModifiedAction = muteAction;
        }

        await _db.SaveChangesAsync();
        await ReplyAsync($"{history.Count} warnings imported.");
        var failedJson = JsonConvert.SerializeObject(failed, CarlSettings);
        await Context.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(failedJson)), "failed.json");
    }

    [Command("dyno")]
    [Summary("Import Dyno warnings into the database.")]
    public async Task ImportDynoWarningsAsync(string link, ModerationCategory? category = null)
    {
        Log.Information("Importing warnings from {Link} into {Category}", link, category?.Name);
        var stream = await _client.GetStringAsync(link);
        var warnings = JsonConvert.DeserializeObject<List<DynoWarning>>(stream);
        if (warnings?.Any() != true)
        {
            await ReplyAsync("No warnings found.");
            return;
        }

        Log.Verbose("Found {Count} warnings", warnings.Count);
        var trackedUsers = new HashSet<ulong>();
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        foreach (var detail in warnings.OrderBy(w => w.Date))
        {
            Log.Information("Importing warning {Detail}", detail.User);

            Log.Verbose("Getting moderator {Moderator}", detail.Moderator);
            if (!trackedUsers.Contains(detail.User))
            {
                Log.Verbose("Getting user {User}", detail.User);
                await _db.Users.TrackUserAsync(detail.User, Context.Guild.Id);

                trackedUsers.Add(detail.User);
            }

            if (!trackedUsers.Contains(detail.Moderator))
            {
                Log.Verbose("Getting moderator {Mod}", detail.Moderator);
                await _db.Users.TrackUserAsync(detail.Moderator, Context.Guild.Id);

                trackedUsers.Add(detail.Moderator);
            }

            Log.Verbose("Creating infraction {User}", detail.User);
            var details = new ReprimandShort(
                detail.User, detail.Moderator,
                Context.Guild.Id, detail.Reason,
                Category: category);
            var expiry = details.Category?.WarningExpiryLength ?? guild.ModerationRules?.WarningExpiryLength;
            var warning = new Warning(1, expiry, details)
            {
                Id        = detail.Id,
                StartedAt = detail.Date.ToUniversalTime(),
                ExpireAt  = detail.Date.ToUniversalTime() + expiry,
                Action    = new ModerationAction(new ActionDetails(detail.Moderator, Context.Guild.Id, details.Reason))
            };

            if (DateTimeOffset.UtcNow >= warning.ExpireAt)
            {
                var ended = warning.ExpireAt ?? DateTimeOffset.UtcNow;

                warning.EndedAt = ended;
                warning.Status  = ReprimandStatus.Expired;
                warning.ModifiedAction = new ModerationAction(
                    new ActionDetails(Dyno, Context.Guild.Id, "[Reprimand Expired]"))
                {
                    Date = ended
                };
            }

            _db.Add(warning);
        }

        await _db.SaveChangesAsync();
        await ReplyAsync($"{warnings.Count} warnings imported.");
    }

    [Command("gaius")]
    [Summary("Import Gaius warnings into the database.")]
    public async Task ImportGaiusWarningsAsync(string link, ModerationCategory? category = null)
    {
        Log.Information("Importing warnings from {Link} into {Category}", link, category?.Name);
        var stream = await _client.GetStringAsync(link);
        var warnings = JsonConvert.DeserializeObject<List<GaiusWarning>>(stream);
        if (warnings?.Any() != true)
        {
            Log.Fatal("No warnings found");
            await ReplyAsync("No warnings found.");
            return;
        }

        if (warnings.Any(w => w.GuildId != Context.Guild.Id))
        {
            Log.Fatal("Gaius warnings are not for this guild");
            await ReplyAsync("Gaius warnings are not for this guild.");
            return;
        }

        Log.Verbose("Found {Count} warnings", warnings.Count);
        var trackedUsers = new HashSet<ulong>();
        var guild = await _db.Guilds.TrackGuildAsync(Context.Guild);
        foreach (var detail in warnings.OrderBy(w => w.WarnId))
        {
            Log.Information("Importing warning {Detail}", detail.UserId);

            if (!trackedUsers.Contains(detail.UserId))
            {
                Log.Verbose("Getting user {User}", detail.UserId);
                await _db.Users.TrackUserAsync(detail.UserId, Context.Guild.Id);

                trackedUsers.Add(detail.UserId);
            }

            if (!trackedUsers.Contains(detail.ModId))
            {
                Log.Verbose("Getting moderator {Mod}", detail.ModId);
                await _db.Users.TrackUserAsync(detail.ModId, Context.Guild.Id);

                trackedUsers.Add(detail.ModId);
            }

            Log.Verbose("Creating infraction {User}", detail.UserId);
            var details = new ReprimandShort(
                detail.UserId, detail.ModId,
                Context.Guild.Id, detail.Reason,
                Category: category);
            var expiry = details.Category?.WarningExpiryLength ?? guild.ModerationRules?.WarningExpiryLength;
            var warning = new Warning(1, expiry, details)
            {
                StartedAt = detail.WarnDate,
                ExpireAt  = detail.PardonDate ?? detail.WarnDate.ToUniversalTime() + expiry,
                Action = new ModerationAction(new ActionDetails(detail.ModId, Context.Guild.Id, details.Reason))
                {
                    Date = detail.WarnDate
                }
            };

            if (DateTimeOffset.UtcNow >= warning.ExpireAt)
            {
                var ended = detail.PardonDate ?? warning.ExpireAt ?? DateTimeOffset.UtcNow;
                var pardonerId = detail.PardonerId ?? Context.Client.CurrentUser.Id;

                warning.EndedAt = ended;
                warning.Status  = pardonerId == Gaius ? ReprimandStatus.Expired : ReprimandStatus.Pardoned;

                if (!trackedUsers.Contains(pardonerId))
                {
                    Log.Verbose("Getting pardoner {Pardoner}", detail.PardonerId);
                    await _db.Users.TrackUserAsync(pardonerId, Context.Guild.Id);

                    trackedUsers.Add(pardonerId);
                }

                warning.ModifiedAction = new ModerationAction(
                    new ActionDetails(pardonerId, Context.Guild.Id,
                        pardonerId == Gaius ? "[Reprimand Expired]" : "[Reprimand Pardoned]"))
                {
                    Date = ended
                };
            }

            Log.Verbose("Added warning for user {User} from {Moderator} which expires {ExpireAt}",
                details.UserId, detail.ModId, warning.ExpireAt.Humanize());

            _db.Add(warning);
        }

        await _db.SaveChangesAsync();
        await ReplyAsync($"{warnings.Count} warnings imported.");
    }

    private enum CarlAction
    {
        Warn,
        Mute,
        Unmute,
        Ban,
        Unban,
        TempBan,
        Kick
    }

    private record DynoWarning(
        DateTimeOffset Date, Guid Id,
        ulong User, ulong Moderator,
        string Reason);

    private record GaiusWarning(
        ulong GuildId, ulong WarnId,
        ulong UserId, string Reason,
        ulong? PardonerId, ulong ModId,
        [JsonConverter(typeof(UnixTimestampConverter))] DateTimeOffset WarnDate,
        [JsonConverter(typeof(UnixTimestampConverter))] DateTimeOffset? PardonDate);

    private class UnixTimestampConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(
            JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            => DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(reader.Value));

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            => writer.WriteValue(((DateTimeOffset) (value ?? DateTimeOffset.UnixEpoch)).ToString("O"));
    }

    private record CarlLog(
        uint CaseId, string? Reason,
        ulong? ModeratorId, ulong OffenderId,
        CarlAction Action, DateTimeOffset Timestamp);
}