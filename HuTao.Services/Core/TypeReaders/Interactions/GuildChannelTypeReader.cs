using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace HuTao.Services.Core.TypeReaders.Interactions;

/// <summary>
///     Reads a channel ID that a custom ID carries. Discord.Net's own reader looks the ID up in every server the bot is
///     in, and a member can send a button with another server's channel, so this reader looks only in the server where
///     the interaction happened.
/// </summary>
public class GuildChannelTypeReader<T> : TypeReader<T> where T : class, IChannel
{
    public override async Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, string option, IServiceProvider services)
    {
        var channel = ulong.TryParse(option, out var id) && context.Guild is not null
            ? await context.Guild.GetChannelAsync(id) as T
            : null;

        return channel is not null
            ? TypeConverterResult.FromSuccess(channel)
            : TypeConverterResult.FromError(InteractionCommandError.ConvertFailed, "That channel is not in this server.");
    }
}
