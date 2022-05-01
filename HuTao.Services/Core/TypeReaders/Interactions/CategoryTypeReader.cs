using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static Discord.Interactions.InteractionCommandError;

namespace HuTao.Services.Core.TypeReaders.Interactions;

public class CategoryTypeReader : TypeReader<ModerationCategory?>
{
    public override async Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, string? option, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(option))
            return TypeConverterResult.FromSuccess(null);

        if (option.Equals("All", StringComparison.OrdinalIgnoreCase))
            return TypeConverterResult.FromSuccess(ModerationCategory.All);

        if (context.Guild is null)
            return TypeConverterResult.FromError(UnmetPrecondition, "This command can only be used in a guild.");

        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        var categories = guild.ModerationCategories;
        var category = categories.FirstOrDefault(c => c.Name.Equals(option, StringComparison.OrdinalIgnoreCase));

        return category is null
            ? TypeConverterResult.FromError(ConvertFailed, "Category not found.")
            : TypeConverterResult.FromSuccess(category);
    }
}