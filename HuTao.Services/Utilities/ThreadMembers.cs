using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace HuTao.Services.Utilities;

/// <summary>A thread that lists its own members, for a thread that is not one of Discord.Net's thread types.</summary>
public interface IThreadMemberList
{
    /// <summary>The IDs of the members in the thread.</summary>
    Task<IReadOnlyCollection<ulong>> GetMemberIdsAsync();
}

/// <summary>
///     Reads who is in a thread. On a thread, Discord.Net answers GetUserAsync and GetUsersAsync of IGuildChannel for the
///     parent channel, so they return everyone who can see the parent channel. These methods read the thread's own member
///     list, and a thread whose member list cannot be read has no members.
/// </summary>
public static class ThreadMembers
{
    /// <summary>The IDs of the members in <paramref name="thread" />.</summary>
    public static async Task<IReadOnlyCollection<ulong>> IdsAsync(IThreadChannel thread) => thread switch
    {
        IThreadMemberList list     => await list.GetMemberIdsAsync(),
        SocketThreadChannel socket => (await socket.GetUsersAsync()).Select(u => u.Id).ToList(),
        RestThreadChannel rest     => (await rest.GetThreadUsersAsync().FlattenAsync()).Select(u => u.Id).ToList(),
        _                          => []
    };

    /// <summary>Whether the member <paramref name="userId" /> is in <paramref name="thread" />.</summary>
    public static async Task<bool> IsMemberAsync(IThreadChannel thread, ulong userId)
        => (await IdsAsync(thread)).Contains(userId);
}
