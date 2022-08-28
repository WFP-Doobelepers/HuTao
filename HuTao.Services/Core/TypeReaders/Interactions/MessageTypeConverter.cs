using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace HuTao.Services.Core.TypeReaders.Interactions;

public class MessageTypeConverter<T> : TypeConverter<T> where T : class, IMessage
{
    private static readonly MessageTypeReader<T> Reader = new();

    public override ApplicationCommandOptionType GetDiscordType() => ApplicationCommandOptionType.String;

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => Reader.ReadAsync(context, option.Value.ToString()!, services);
}

public class MessageComponentTypeConverter<T> : ComponentTypeConverter<T> where T : class, IMessage
{
    private static readonly MessageTypeReader<T> Reader = new();

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IComponentInteractionData option, IServiceProvider services)
        => Reader.ReadAsync(context, option.Value, services);
}