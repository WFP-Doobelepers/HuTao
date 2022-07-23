using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation.Auto.Configurations;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static Discord.Commands.CommandError;

namespace HuTao.Services.Core.TypeReaders.Commands;

public class SpamTypeReader : EntityTypeReader<AutoConfiguration>
{
    public override async Task<TypeReaderResult> ReadAsync(
        ICommandContext context, string input, IServiceProvider services)
    {
        if (context.Guild is null)
            return TypeReaderResult.FromError(UnmetPrecondition, "This command can only be used in a guild.");

        return await base.ReadAsync(context, input, services);
    }

    protected override EmbedBuilder EntityViewer(AutoConfiguration entity) => throw new NotImplementedException();

    protected override string Id(AutoConfiguration entity) => entity.Id.ToString();

    protected override async Task<ICollection<AutoConfiguration>?> GetCollectionAsync(
        Context context, IServiceProvider services)
    {
        var db = services.GetRequiredService<HuTaoContext>();
        var guild = await db.Guilds.TrackGuildAsync(context.Guild);

        return guild.ModerationRules?.Triggers.OfType<AutoConfiguration>().ToList();
    }
}