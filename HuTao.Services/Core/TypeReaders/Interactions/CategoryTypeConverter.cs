using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data.Models.Moderation;

namespace HuTao.Services.Core.TypeReaders.Interactions;

public class CategoryTypeConverter : TypeConverter<ModerationCategory?>
{
    private static readonly CategoryTypeReader Reader = new();

    public override ApplicationCommandOptionType GetDiscordType() => ApplicationCommandOptionType.String;

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        => Reader.ReadAsync(context, option.Value.ToString()!, services);
}

public class CategoryComponentTypeConverter : ComponentTypeConverter<ModerationCategory?>
{
    private static readonly CategoryTypeReader Reader = new();

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IComponentInteractionData option, IServiceProvider services)
        => Reader.ReadAsync(context, option.Value, services);
}