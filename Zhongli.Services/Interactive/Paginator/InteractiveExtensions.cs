using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Addons.Interactive.Criteria;
using Discord.Addons.Interactive.Paginator;
using Discord.Commands;
using Discord.WebSocket;

namespace Zhongli.Services.Interactive.Paginator
{
    public static class InteractiveExtensions
    {
        public static EmbedBuilder ToEmbed(this PaginatedMessage paginated)
        {
            var embed = new EmbedBuilder()
                .WithColor(paginated.Color);

            if (paginated.Author is not null)
                embed.WithAuthor(paginated.Author);

            if (!string.IsNullOrEmpty(paginated.Title))
                embed.WithAuthor(paginated.Title);

            if (!string.IsNullOrEmpty(paginated.AlternateDescription))
                embed.WithDescription(paginated.AlternateDescription);

            embed.Fields = paginated.Pages.Cast<EmbedFieldBuilder>().ToList();

            return embed;
        }

        public static EmbedBuilder ToEmbed(this PaginatedMessage paginated, out string content)
        {
            content = paginated.Content;

            return paginated.ToEmbed();
        }

        public static Task<IUserMessage> PagedDMAsync<T>(
            this InteractiveBase<T> interactive, PaginatedMessage pager,
            bool fromSourceUser = true) where T : SocketCommandContext
        {
            var criterion = new Criteria<SocketReaction>();

            if (fromSourceUser)
                criterion.AddCriterion(new EnsureReactionFromSourceUserCriterion());

            return SendPaginatedDMAsync(interactive.Interactive, interactive.Context, pager, criterion);
        }

        private static async Task<IUserMessage> SendPaginatedDMAsync(
            this InteractiveService interactive,
            SocketCommandContext context, PaginatedMessage pager,
            ICriterion<SocketReaction>? criterion = null)
        {
            var callback = new PaginatedDMCallback(interactive, context, pager);
            await callback.DisplayAsync().ConfigureAwait(false);

            return callback.Message;
        }

        private class EnsureReactionFromSourceUserCriterion : ICriterion<SocketReaction>
        {
            public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketReaction parameter)
                => Task.FromResult(parameter.UserId == sourceContext.User.Id);
        }
    }
}