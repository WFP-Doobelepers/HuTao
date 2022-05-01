using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static Discord.Commands.CommandError;

namespace HuTao.Services.Core.TypeReaders.Commands;

public class CategoryTypeReader : TypeReader
{
    public override async Task<TypeReaderResult> ReadAsync(
        ICommandContext context, string input, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(input))
            return TypeReaderResult.FromSuccess(null);

        if (input.Equals("All", StringComparison.OrdinalIgnoreCase))
            return TypeReaderResult.FromSuccess(ModerationCategory.All);

        if (context.Guild is null)
            return TypeReaderResult.FromError(UnmetPrecondition, "This command can only be used in a guild.");

        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        var categories = guild.ModerationCategories.Append(ModerationCategory.All);
        var category = categories.FirstOrDefault(c => c.Name.Equals(input, StringComparison.OrdinalIgnoreCase));

        return category is null
            ? TypeReaderResult.FromError(ParseFailed, "Category not found.")
            : TypeReaderResult.FromSuccess(category);
    }
}