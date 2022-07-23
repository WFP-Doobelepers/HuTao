using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Logging;
using HuTao.Services.Core;
using HuTao.Services.Image;
using HuTao.Services.Interactive.Paginator;
using HuTao.Services.Utilities;
using static Discord.InteractionResponseType;

namespace HuTao.Services.Moderation;

public class UserService
{
    private const AuthorizationScope Scope = AuthorizationScope.All | AuthorizationScope.History;
    private readonly AuthorizationService _auth;
    private readonly HuTaoContext _db;
    private readonly IImageService _image;
    private readonly InteractiveService _interactive;

    public UserService(
        AuthorizationService auth, IImageService image,
        InteractiveService interactive, HuTaoContext db)
    {
        _auth        = auth;
        _image       = image;
        _interactive = interactive;
        _db          = db;
    }

    public async Task ReplyAvatarAsync(Context context, IUser user, bool ephemeral = false)
    {
        await context.DeferAsync(ephemeral);
        var components = await ComponentsAsync(context, user);
        var avatar = user.GetDefiniteAvatarUrl(4096);
        var embed = new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId)
            .WithImageUrl(avatar)
            .WithColor(await _image.GetAvatarColor(user))
            .WithUserAsAuthor(context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        if (user is IGuildUser guild)
        {
            var guildAvatar = guild.GetGuildAvatarUrl(size: 4096);
            if (guildAvatar is not null) embed.WithThumbnailUrl(avatar).WithImageUrl(guildAvatar);
        }

        await context.ReplyAsync(embed: embed.Build(), components: components, ephemeral: ephemeral);
    }

    public async Task ReplyHistoryAsync(
        Context context, ModerationCategory? category,
        LogReprimandType type, IUser user,
        bool update, bool ephemeral = false)
    {
        await context.DeferAsync(ephemeral);

        var userEntity = await _db.Users.TrackUserAsync(user, context.Guild);
        var guild = await _db.Guilds.TrackGuildAsync(context.Guild);

        if (type is LogReprimandType.None)
        {
            type = category?.Logging?.HistoryReprimands
                ?? guild.ModerationRules?.Logging?.HistoryReprimands
                ?? LogReprimandType.None;
        }

        var history = guild.ReprimandHistory
            .Where(r => r.UserId == user.Id)
            .OfType(type).OfCategory(category);

        var reprimands = history
            .OrderByDescending(r => r.Action?.Date).Select(r => r.ToEmbedBuilder(true))
            .Prepend(GetReprimands(userEntity, category)
                .WithColor(await _image.GetAvatarColor(user))
                .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail));

        var pages = reprimands.Chunk(4)
            .Select(builders =>
            {
                builders.First().WithUserAsAuthor(user, AuthorOptions.IncludeId);
                return new MultiEmbedPageBuilder().WithBuilders(builders);
            });

        var paginator = InteractiveExtensions.CreateDefaultPaginator().WithPages(pages).Build();

        await (context switch
        {
            CommandContext command => _interactive.SendPaginatorAsync(paginator, command.Channel,
                messageAction: Components),

            InteractionContext { Interaction: SocketInteraction interaction }
                => _interactive.SendPaginatorAsync(paginator, interaction,
                    ephemeral: ephemeral,
                    responseType: update ? DeferredUpdateMessage : DeferredChannelMessageWithSource,
                    messageAction: Components),

            _ => throw new ArgumentOutOfRangeException(
                nameof(context), context, "Invalid context.")
        });

        void Components(IUserMessage message)
        {
            var components = GetMessageComponents(message).WithSelectMenu(HistoryMenu(user, category, type));
            if (guild.ModerationCategories.Any())
                components.WithSelectMenu(CategoryMenu(user, guild.ModerationCategories, category, type));

            components.WithSelectMenu(ReprimandMenu(user));

            _ = message switch
            {
                RestInteractionMessage m => m.ModifyAsync(r => r.Components       = components.Build()),
                RestFollowupMessage m    => m.ModifyAsync(r => r.Components       = components.Build()),
                _                        => message.ModifyAsync(r => r.Components = components.Build())
            };
        }

        ComponentBuilder GetMessageComponents(IMessage message)
        {
            var components = ComponentBuilder.FromMessage(message);
            components.ActionRows?.RemoveAll(row => row.Components.Any(c => c.Type is ComponentType.SelectMenu));

            return components;
        }
    }

    public async Task ReplyUserAsync(Context context, IUser user, bool ephemeral = false)
    {
        await context.DeferAsync(ephemeral);

        var components = await ComponentsAsync(context, user);
        var builders = await GetUserAsync(context, user);
        var embeds = builders.Select(e => e.Build()).ToArray();

        await context.ReplyAsync(components: components, embeds: embeds, ephemeral: ephemeral);
    }

    private static EmbedBuilder GetReprimands(GuildUserEntity user, ModerationCategory? category)
    {
        var rules = category?.Logging?.SummaryReprimands
            ?? user.Guild.ModerationRules?.Logging?.SummaryReprimands
            ?? LogReprimandType.All;

        var embed = new EmbedBuilder().WithTitle(category is null
            ? "Reprimands [Active/Total]"
            : "Reprimands [Active/Total] [Global]");

        if (rules.HasFlag(LogReprimandType.Warning))
            embed.AddField(Warnings(user, category));

        if (rules.HasFlag(LogReprimandType.Notice))
            embed.AddField(Reprimands<Notice>(user, category));

        if (rules.HasFlag(LogReprimandType.Ban))
            embed.AddField(Reprimands<Ban>(user, category));

        if (rules.HasFlag(LogReprimandType.Kick))
            embed.AddField(Reprimands<Kick>(user, category));

        if (rules.HasFlag(LogReprimandType.Note))
            embed.AddField(Reprimands<Note>(user, category));

        if (rules.HasFlag(LogReprimandType.Mute))
            embed.AddField(Reprimands<Mute>(user, category));

        if (rules.HasFlag(LogReprimandType.Censored))
            embed.AddField(Reprimands<Censored>(user, category));

        return embed;
    }

    private static EmbedFieldBuilder Reprimands<T>(GuildUserEntity user, ModerationCategory? category)
        where T : Reprimand
    {
        var global = user.HistoryCount<T>(null);
        var count = user.HistoryCount<T>(category);

        var embed = new EmbedFieldBuilder()
            .WithName(typeof(T).Name)
            .WithIsInline(true);

        return category is null
            ? embed.WithValue($"{count.Active}/{count.Total}")
            : embed.WithValue($"{count.Active}/{count.Total} [{global.Active}/{global.Total}]");
    }

    private static EmbedFieldBuilder Warnings(GuildUserEntity user, ModerationCategory? category)
    {
        var global = user.WarningCount(null);
        var count = user.WarningCount(category);

        var embed = new EmbedFieldBuilder()
            .WithName(nameof(Warning))
            .WithIsInline(true);

        return category is null
            ? embed.WithValue($"{count.Active}/{count.Total}")
            : embed.WithValue($"{count.Active}/{count.Total} [{global.Active}/{global.Total}]");
    }

    private static SelectMenuBuilder CategoryMenu(
        IUser user, IEnumerable<ModerationCategory> categories,
        ModerationCategory? selected, LogReprimandType type = LogReprimandType.None)
        => new SelectMenuBuilder()
            .WithCustomId($"category:{user.Id}:{(int) type}")
            .WithPlaceholder("Select a category")
            .WithMinValues(0).WithMaxValues(1)
            .WithOptions(categories
                .Append(ModerationCategory.Default)
                .Select(c => new SelectMenuOptionBuilder(c.Name, c.Name, isDefault: selected?.Name == c.Name))
                .ToList());

    private static SelectMenuBuilder HistoryMenu(IUser user,
        ModerationCategory? category = null, LogReprimandType type = LogReprimandType.None)
    {
        var types = Enum.GetValues<LogReprimandType>()[1..^1];

        var menu = new SelectMenuBuilder()
            .WithCustomId($"reprimand:{user.Id}:{category?.Name}")
            .WithPlaceholder("View History")
            .WithMinValues(1).WithMaxValues(types.Length);

        foreach (var e in types)
        {
            var name = e.ToString();
            var title = e.Humanize(LetterCasing.Title);
            var selected = type.HasFlag(e) && type is not LogReprimandType.None;
            menu.AddOption(title, name, $"View {title} history", isDefault: selected);
        }

        return menu;
    }

    private static SelectMenuBuilder ReprimandMenu(IUser user)
    {
        var menu = new SelectMenuBuilder()
            .WithMinValues(1).WithMaxValues(1)
            .WithCustomId($"mod-menu:{user.Id}")
            .WithPlaceholder("Mod Menu")
            .AddOption("Ban", nameof(LogReprimandType.Ban), "Ban the user")
            .AddOption("Note", nameof(LogReprimandType.Note), "Add a note to the user");

        if (user is IGuildUser)
        {
            menu.AddOption("Warn", nameof(LogReprimandType.Warning), "Warn the user")
                .AddOption("Kick", nameof(LogReprimandType.Kick), "Kick the user")
                .AddOption("Mute", nameof(LogReprimandType.Mute), "Mute the user")
                .AddOption("Hard Mute", nameof(LogReprimandType.HardMute), "Hard Mute the user");
        }

        return menu;
    }

    private async Task<IEnumerable<EmbedBuilder>> GetUserAsync(Context context, IUser user)
    {
        var isAuthorized =
            await _auth.IsAuthorizedAsync(context, Scope) ||
            await _auth.IsCategoryAuthorizedAsync(context, Scope);

        var userEntity = await _db.Users.FindAsync(user.Id, context.Guild.Id);
        var guildUser = user as SocketGuildUser;

        var embeds = new List<EmbedBuilder>();
        var embed = new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId | AuthorOptions.UseThumbnail)
            .WithUserAsAuthor(context.User, AuthorOptions.UseFooter | AuthorOptions.Requested)
            .WithDescription(user.Mention)
            .AddField("Created", user.CreatedAt.ToUniversalTimestamp());
        embeds.Add(embed);

        if (userEntity?.JoinedAt is not null)
            embed.AddField("First Joined", userEntity.JoinedAt.Value.ToUniversalTimestamp());

        if (guildUser is not null)
        {
            if (guildUser.JoinedAt is not null)
                embed.AddField("Joined", guildUser.JoinedAt.Value.ToUniversalTimestamp());

            var roles = guildUser.Roles
                .OrderByDescending(r => r.Position)
                .ToList();

            embed
                .WithColor(roles.Select(r => r.Color).FirstOrDefault(c => c.RawValue is not 0))
                .AddItemsIntoFields($"Roles [{guildUser.Roles.Count}]", roles.Select(r => r.Mention), " ");

            if (isAuthorized)
            {
                if (guildUser.TimedOutUntil is not null)
                    embed.AddField("Timeout", guildUser.TimedOutUntil.Humanize());

                var mute = await _db.GetActive<Mute>(guildUser);
                if (mute is not null) embed.AddField("Muted", mute.ExpireAt.Humanize(), true);
            }
        }

        var ban = await context.Guild.GetBanAsync(user);
        if (!isAuthorized || ban is null) return embeds;

        embed.WithColor(Color.Red);
        var banDetails = userEntity?.Reprimands<Ban>(null).MaxBy(b => b.Action?.Date);
        if (banDetails is not null)
            embeds.Add(banDetails.ToEmbedBuilder(true));
        else
            embed.AddField("Banned", $"This user is banned. Reason: {ban.Reason ?? "None"}");

        return embeds;
    }

    private async Task<MessageComponent?> ComponentsAsync(Context context, IUser user)
    {
        var auth = await _auth.IsAuthorizedAsync(context, Scope);
        var category = await _auth.IsCategoryAuthorizedAsync(context, Scope);
        return auth || category
            ? new ComponentBuilder()
                .WithSelectMenu(HistoryMenu(user))
                .WithSelectMenu(ReprimandMenu(user))
                .Build()
            : null;
    }
}