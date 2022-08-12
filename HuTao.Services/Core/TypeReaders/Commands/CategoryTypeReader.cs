using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static Discord.Commands.CommandError;

namespace HuTao.Services.Core.TypeReaders.Commands;

public class CategoryTypeReader : EntityTypeReader<ModerationCategory>
{
    public override async Task<TypeReaderResult> ReadAsync(
        ICommandContext context, string input, IServiceProvider services)
    {
        if (input.Equals("Default", StringComparison.OrdinalIgnoreCase))
            return TypeReaderResult.FromSuccess(ModerationCategory.Default);

        if (context.Guild is null)
            return TypeReaderResult.FromError(UnmetPrecondition, "This command can only be used in a guild.");

        return await base.ReadAsync(context, input, services);
    }

    protected override EmbedBuilder EntityViewer(ModerationCategory entity) => entity.ToEmbedBuilder();

    protected override string Id(ModerationCategory entity) => entity.Name;

    protected override async Task<ICollection<ModerationCategory>?> GetCollectionAsync(
        Context context, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        return guild.ModerationCategories;
    }
}