﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using HuTao.Data;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Moderation;
using HuTao.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static Discord.Interactions.InteractionCommandError;

namespace HuTao.Services.Core.TypeReaders.Interactions;

public class CategoryTypeReader : EntityTypeReader<ModerationCategory>
{
    public override async Task<TypeConverterResult> ReadAsync(
        IInteractionContext context, string option, IServiceProvider services)
    {
        if (context.Guild is null)
            return TypeConverterResult.FromError(UnmetPrecondition, "This command can only be used in a guild.");

        if (string.IsNullOrEmpty(option)
            || option.Equals("null", StringComparison.OrdinalIgnoreCase)
            || option.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            var db = services.GetRequiredService<HuTaoContext>();
            var user = await db.Users.TrackUserAsync(context.User, context.Guild);
            return TypeConverterResult.FromSuccess(user.DefaultCategory ?? ModerationCategory.None);
        }

        if (option.Equals("All", StringComparison.OrdinalIgnoreCase))
            return TypeConverterResult.FromSuccess(ModerationCategory.All);

        return await base.ReadAsync(context, option, services);
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