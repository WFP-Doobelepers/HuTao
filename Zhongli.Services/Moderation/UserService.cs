using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Fergun.Interactive;
using Humanizer;
using Zhongli.Data;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Discord;
using Zhongli.Data.Models.Moderation.Infractions.Reprimands;
using Zhongli.Data.Models.Moderation.Logging;
using Zhongli.Services.Core;
using Zhongli.Services.Image;
using Zhongli.Services.Interactive.Paginator;
using Zhongli.Services.Utilities;
using static Discord.InteractionResponseType;

namespace Zhongli.Services.Moderation;

public class UserService
{
    private readonly AuthorizationService _auth;
    private readonly IImageService _image;
    private readonly InteractiveService _interactive;
    private readonly ZhongliContext _db;

    public UserService(
        AuthorizationService auth, IImageService image,
        InteractiveService interactive, ZhongliContext db)
    {
        _auth        = auth;
        _image       = image;
        _interactive = interactive;
        _db          = db;
    }

    public async Task ReplyAvatarAsync(Context context, IUser user)
    {
        await context.DeferAsync(true);
        var components = await ComponentsAsync(context, user);
        var embed = new EmbedBuilder()
            .WithUserAsAuthor(user, AuthorOptions.IncludeId)
            .WithImageUrl(user.GetDefiniteAvatarUrl(2048))
            .WithColor(await _image.GetAvatarColor(user))
            .WithUserAsAuthor(context.User, AuthorOptions.UseFooter | AuthorOptions.Requested);

        await context.ReplyAsync(embed: embed.Build(), ephemeral: true, components: components);
    }

    public async Task ReplyHistoryAsync(Context context, LogReprimandType type, IUser user, bool update)
    {
        var userEntity = _db.Users.FirstOrDefault(u => u.Id == user.Id && u.GuildId == context.Guild.Id);
        if (userEntity is null) return;

        var guild = await _db.Guilds.TrackGuildAsync(context.Guild);
        var history = guild.ReprimandHistory
            .Where(u => u.UserId == user.Id)
            .OfType(type);

        var reprimands = history
            .OrderByDescending(r => r.Action?.Date)
            .Select(r => r.ToEmbedBuilder(true))
            .ToList();

        reprimands.Insert(0, GetReprimands(userEntity)
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
                    ephemeral: true,
                    responseType: update ? UpdateMessage : ChannelMessageWithSource,
                    messageAction: Components),

            _ => throw new ArgumentOutOfRangeException(
                nameof(context), context, "Invalid context.")
        });

        async void Components(IUserMessage m)
        {
            var menu = SelectMenu(user, type);
            var components = ComponentBuilder.FromMessage(m).WithSelectMenu(menu).Build();

            if (m is RestInteractionMessage interaction)
                await interaction.ModifyAsync(r => r.Components = components);
            else
                await m.ModifyAsync(r => r.Components = components);
        }
    }

    public async Task ReplyUserAsync(Context context, IUser user)
    {
        var components = await ComponentsAsync(context, user);
        var builders = await GetUserAsync(context, user);
        var embeds = builders.Select(e => e.Build()).ToArray();

        await context.ReplyAsync(embeds: embeds, ephemeral: true, components: components);
    }

    private static EmbedBuilder GetReprimands(GuildUserEntity user) => new EmbedBuilder()
        .WithTitle("Reprimands [Active/Total]")
        .AddField(Warnings(user))
        .AddField(Reprimands<Notice>(user))
        .AddField(Reprimands<Ban>(user))
        .AddField(Reprimands<Kick>(user))
        .AddField(Reprimands<Note>(user))
        .AddField(Reprimands<Mute>(user))
        .AddField(Reprimands<Censored>(user));

    private static EmbedFieldBuilder Reprimands<T>(GuildUserEntity user)
        where T : Reprimand => new EmbedFieldBuilder()
        .WithName(typeof(T).Name)
        .WithValue($"{user.HistoryCount<T>(false)}/{user.HistoryCount<T>()}")
        .WithIsInline(true);

    private static EmbedFieldBuilder Warnings(GuildUserEntity user) => new EmbedFieldBuilder()
        .WithName(nameof(Warning))
        .WithValue($"{user.WarningCount(false)}/{user.WarningCount()}")
        .WithIsInline(true);

    private static SelectMenuBuilder SelectMenu(IUser user, LogReprimandType type = LogReprimandType.None)
    {
        var types = Enum.GetValues<LogReprimandType>()[1..^1];

        var menu = new SelectMenuBuilder()
            .WithCustomId($"r:{user.Id}")
            .WithPlaceholder("Select an infraction")
            .WithMinValues(1).WithMaxValues(types.Length);

        foreach (var e in types)
        {
            var name = e.ToString();
            var selected = type.HasFlag(e);
            menu.AddOption(name, name, isDefault: selected);
        }

        return menu;
    }

    private async Task<IEnumerable<EmbedBuilder>> GetUserAsync(Context context, IUser user)
    {
        var isAuthorized = await _auth
            .IsAuthorizedAsync(context, AuthorizationScope.All | AuthorizationScope.Moderator);
        var userEntity = await _db.Users.FindAsync(user.Id, context.Guild.Id);
        var guild = await _db.Guilds.TrackGuildAsync(context.Guild);
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
                .AddField($"Roles [{guildUser.Roles.Count}]", roles.Humanize(r => r.Mention));

            if (isAuthorized)
            {
                if (guildUser.TimedOutUntil is not null)
                    embed.AddField("Timeout", guildUser.TimedOutUntil.Humanize());
                else if (guild.ModerationRules.MuteRoleId is not null)
                {
                    var mute = await _db.GetActive<Mute>(guildUser);
                    if (mute is not null) embed.AddField("Muted", mute.ExpireAt.Humanize(), true);
                }
            }
        }

        var ban = await context.Guild.GetBanAsync(user);
        if (!isAuthorized || ban is null) return embeds;

        embed.WithColor(Color.Red);

        var banDetails = userEntity?.Reprimands<Ban>()
            .OrderByDescending(b => b.Action?.Date)
            .FirstOrDefault();

        if (banDetails is not null)
            embeds.Add(banDetails.ToEmbedBuilder(true));
        else
            embed.AddField("Banned", $"This user is banned. Reason: {ban.Reason ?? "None"}");

        if (userEntity is not null)
            embeds.Add(GetReprimands(userEntity));

        return embeds;
    }

    private async Task<MessageComponent?> ComponentsAsync(Context context, IUser user)
    {
        var authorized = await _auth.IsAuthorizedAsync(context, AuthorizationScope.All | AuthorizationScope.Helper);
        return authorized ? new ComponentBuilder().WithSelectMenu(SelectMenu(user)).Build() : null;
    }
}