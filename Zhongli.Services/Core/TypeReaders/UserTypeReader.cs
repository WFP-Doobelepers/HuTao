using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Zhongli.Services.Core.TypeReaders
{
    /// <summary>
    ///     A <see cref="TypeReader" /> for parsing objects implementing <see cref="IUser" />.
    /// </summary>
    /// <typeparam name="T">The type to be checked; must implement <see cref="IUser" />.</typeparam>
    public class UserTypeReader<T> : TypeReader
        where T : class, IUser
    {
        private readonly bool _useRest;
        private readonly CacheMode _cacheMode;

        public UserTypeReader(CacheMode cacheMode = CacheMode.CacheOnly, bool useRest = false)
        {
            _cacheMode = cacheMode;
            _useRest   = useRest;
        }

        /// <inheritdoc />
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            var results = new Dictionary<ulong, TypeReaderValue>();
            var channelUsers = context.Channel.GetUsersAsync(_cacheMode).Flatten();
            IReadOnlyCollection<IGuildUser> guildUsers = ImmutableArray.Create<IGuildUser>();

            if (context.Guild != null)
                guildUsers = await context.Guild.GetUsersAsync(_cacheMode).ConfigureAwait(false);

            //By Mention (1.0)
            if (MentionUtils.TryParseUser(input, out var id))
            {
                if (context.Guild != null)
                {
                    var guildUser = await context.Guild.GetUserAsync(id, _cacheMode).ConfigureAwait(false);
                    var user = await GetUserAsync(context.Client, guildUser, id);

                    AddResult(results, user, 1.00f);
                }
                else
                {
                    var channelUser = await context.Channel.GetUserAsync(id, _cacheMode).ConfigureAwait(false);
                    var user = await GetUserAsync(context.Client, channelUser, id);

                    AddResult(results, user, 1.00f);
                }
            }

            //By Id (0.9)
            if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                if (context.Guild != null)
                {
                    var guildUser = await context.Guild.GetUserAsync(id, _cacheMode).ConfigureAwait(false);
                    var user = await GetUserAsync(context.Client, guildUser, id);

                    AddResult(results, user, 0.90f);
                }
                else
                {
                    var channelUser = await context.Channel.GetUserAsync(id, _cacheMode).ConfigureAwait(false);
                    var user = await GetUserAsync(context.Client, channelUser, id);

                    AddResult(results, user, 1.00f);
                }
            }

            //By Username + Discriminator (0.7-0.85)
            var index = input.LastIndexOf('#');
            if (index >= 0)
            {
                var username = input[..index];
                if (ushort.TryParse(input[(index + 1)..], out var discriminator))
                {
                    var channelUser = await channelUsers
                        .Where(x => x.DiscriminatorValue == discriminator)
                        .Where(x => string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);

                    AddResult(results, channelUser as T, channelUser?.Username == username ? 0.85f : 0.75f);

                    var guildUser = guildUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
                        string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase));
                    AddResult(results, guildUser as T, guildUser?.Username == username ? 0.80f : 0.70f);
                }
            }

            //By Username (0.5-0.6)
            {
                await channelUsers
                    .Where(x => string.Equals(input, x.Username, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser
                        => AddResult(results, channelUser as T, channelUser.Username == input ? 0.65f : 0.55f))
                    .ConfigureAwait(false);

                foreach (var guildUser in guildUsers.Where(x
                    => string.Equals(input, x.Username, StringComparison.OrdinalIgnoreCase)))
                {
                    AddResult(results, guildUser as T, guildUser.Username == input ? 0.60f : 0.50f);
                }
            }

            //By Nickname (0.5-0.6)
            {
                await channelUsers
                    .Where(x => string.Equals(input, (x as IGuildUser)?.Nickname, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser => AddResult(results, channelUser as T,
                        (channelUser as IGuildUser)?.Nickname == input ? 0.65f : 0.55f))
                    .ConfigureAwait(false);

                foreach (var guildUser in guildUsers.Where(x
                    => string.Equals(input, x.Nickname, StringComparison.OrdinalIgnoreCase)))
                {
                    AddResult(results, guildUser as T, guildUser.Nickname == input ? 0.60f : 0.50f);
                }
            }

            return results.Count > 0
                ? TypeReaderResult.FromSuccess(results.Values.ToImmutableArray())
                : TypeReaderResult.FromError(CommandError.ObjectNotFound, "User not found.");
        }

        private async Task<T?> GetUserAsync(IDiscordClient client, IUser? user, ulong id)
        {
            var result = user as T;
            if (user is not null || !_useRest)
                return result;

            if (client is not DiscordSocketClient socketClient) return null;
            return socketClient.GetUser(id) as T ?? await socketClient.Rest.GetUserAsync(id).ConfigureAwait(false) as T;
        }

        private static void AddResult(IDictionary<ulong, TypeReaderValue> results, T? user, float score)
        {
            if (user is not null && !results.ContainsKey(user.Id))
                results.Add(user.Id, new TypeReaderValue(user, score));
        }
    }
}