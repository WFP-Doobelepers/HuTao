using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data.Models.Authorization;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Moderation;

public class PurgeModule : ModuleBase<SocketCommandContext>
{
    [Command("purge")]
    [Alias("clean")]
    [Summary("Purges messages based on set rules.")]
    [RequireAuthorization(AuthorizationScope.Purge)]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    [RequireBotPermission(ChannelPermission.ManageMessages)]
    public async Task PurgeAsync(int amount, PurgeFilters? options = null)
    {
        var messages = await Context.Channel
            .GetMessagesAsync(Context.Message, Direction.Before, amount)
            .Flatten().ToListAsync();

        var channel = (ITextChannel) Context.Channel;
        if (options is null)
            await channel.DeleteMessagesAsync(messages);
        else
        {
            var rules = options.GetRules();
            var result = options.FilterMode switch
            {
                PurgeFilters.FilterType.All      => messages.Where(m => rules.All(rule => rule(m))),
                PurgeFilters.FilterType.Any or _ => messages.Where(m => rules.Any(rule => rule(m)))
            };

            if (options.Invert ?? false)
                result = messages.Except(result);

            var deleted = result.ToList();
            await channel.DeleteMessagesAsync(deleted);
            amount = deleted.Count;
        }

        await ReplyAsync($"Deleted {amount} messages.");
    }

    [NamedArgumentType]
    public class PurgeFilters
    {
        public enum FilterType
        {
            [HelpSummary("Match any of the rules.")]
            Any,

            [HelpSummary("Match all of the rules.")]
            All
        }

        [HelpSummary("Include messages with attachments.")]
        public bool? HasAttachments { get; set; }

        [HelpSummary("Include message with embeds.")]
        public bool? HasEmbeds { get; set; }

        [HelpSummary("Any roles that are mentioned in the message.")]
        public bool? HasInvites { get; set; }

        [HelpSummary("Invert the results.")] public bool? Invert { get; set; }

        [HelpSummary("Include messages with bots.")]
        public bool? IsBot { get; set; }

        [HelpSummary("Defaults to `Any`.")] public FilterType FilterMode { get; set; }

        [HelpSummary("Include messages that mention these roles.")]
        public IEnumerable<IRole>? MentionedRoles { get; set; }

        [HelpSummary("Include message authors that contain these roles.")]
        public IEnumerable<IRole>? Roles { get; set; }

        [HelpSummary("Include messages that mention these users.")]
        public IEnumerable<IUser>? MentionedUsers { get; set; }

        [HelpSummary("Include messages that contain these users.")]
        public IEnumerable<IUser>? Users { get; set; }

        [HelpSummary("Include messages that contain this string.")]
        public string? Contains { get; set; }

        [HelpSummary("Include messages that ends with this string.")]
        public string? EndsWith { get; set; }

        [HelpSummary("Include messages that match this regex pattern. Ignores case.")]
        public string? RegexPattern { get; set; }

        [HelpSummary("Include messages that start with this string.")]
        public string? StartsWith { get; set; }

        public IEnumerable<Func<IMessage, bool>> GetRules()
        {
            if (HasAttachments is not null)
            {
                yield return m =>
                {
                    var hasAttachments = m.Attachments.Any();
                    return HasAttachments.Value == hasAttachments;
                };
            }

            if (HasEmbeds is not null)
            {
                yield return m =>
                {
                    var hasEmbeds = m.Embeds.Any();
                    return HasEmbeds.Value == hasEmbeds;
                };
            }

            if (HasInvites is not null)
            {
                yield return m =>
                {
                    var match = RegexUtilities.Invite.IsMatch(m.Content);
                    return HasInvites.Value == match;
                };
            }

            if (IsBot is not null)
            {
                yield return m =>
                {
                    var isBot = m.Author.IsBot || m.Author.IsWebhook;
                    return IsBot.Value == isBot;
                };
            }

            if (MentionedRoles is not null)
                yield return m => m.MentionedRoleIds.Intersect(MentionedRoles.Select(r => r.Id)).Any();

            if (Roles is not null)
            {
                yield return m =>
                    m.Author is IGuildUser user
                    && Roles.Select(r => r.Id).Intersect(user.RoleIds).Any();
            }

            if (MentionedUsers is not null)
                yield return m => m.MentionedUserIds.Intersect(MentionedUsers.Select(r => r.Id)).Any();

            if (Users is not null)
                yield return m => Users.Any(u => u.Id == m.Author.Id);

            if (Contains is not null)
                yield return m => m.Content.Contains(Contains);

            if (EndsWith is not null)
                yield return m => m.Content.EndsWith(EndsWith);

            if (StartsWith is not null)
                yield return m => m.Content.StartsWith(StartsWith);

            if (RegexPattern is not null)
            {
                yield return m => Regex.IsMatch(m.Content, RegexPattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            }
        }
    }
}