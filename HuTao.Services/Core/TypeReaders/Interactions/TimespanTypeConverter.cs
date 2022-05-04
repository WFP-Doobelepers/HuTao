using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace HuTao.Services.Core.TypeReaders.Interactions;

internal class TimeSpanTypeReader : ComponentTypeConverter<TimeSpan>
{
    /// <summary>
    ///     TimeSpan try parse formats.
    /// </summary>
    private static readonly string[] Formats =
    {
        "%d'd'%h'h'%m'm'%s's'", // 4d3h2m1s
        "%d'd'%h'h'%m'm'",      // 4d3h2m
        "%d'd'%h'h'%s's'",      // 4d3h  1s
        "%d'd'%h'h'",           // 4d3h
        "%d'd'%m'm'%s's'",      // 4d  2m1s
        "%d'd'%m'm'",           // 4d  2m
        "%d'd'%s's'",           // 4d    1s
        "%d'd'",                // 4d
        "%h'h'%m'm'%s's'",      //   3h2m1s
        "%h'h'%m'm'",           //   3h2m
        "%h'h'%s's'",           //   3h  1s
        "%h'h'",                //   3h
        "%m'm'%s's'",           //     2m1s
        "%m'm'",                //     2m
        "%s's'"                 //       1s
    };

    public override Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, IComponentInteractionData option, IServiceProvider services)
    {
        var input = option.Value;
        if (string.IsNullOrEmpty(input))
            return Task.FromResult(TypeConverterResult.FromSuccess(TimeSpan.FromMinutes(30)));

        var isNegative = input[0] == '-'; // Char for CultureInfo.InvariantCulture.NumberFormat.NegativeSign
        if (isNegative) input = input[1..];

        return TimeSpan.TryParseExact(input.ToLowerInvariant(), Formats, CultureInfo.InvariantCulture, out var timeSpan)
            ? isNegative
                ? Task.FromResult(TypeConverterResult.FromSuccess(-timeSpan))
                : Task.FromResult(TypeConverterResult.FromSuccess(timeSpan))
            : Task.FromResult(TypeConverterResult.FromError(
                InteractionCommandError.ConvertFailed,
                "Failed to parse TimeSpan"));
    }
}