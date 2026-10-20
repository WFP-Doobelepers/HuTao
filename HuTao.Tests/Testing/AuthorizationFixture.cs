using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Moderation;
using HuTao.Services.Core;
using Microsoft.EntityFrameworkCore;
using Moq;
using GuildPermission = HuTao.Data.Models.Discord.GuildPermission;

namespace HuTao.Tests.Testing;

/// <summary>
///     One guild and its authorization rules, served to a real <see cref="AuthorizationService" /> by a
///     <see cref="HuTaoContext" /> whose tables are mocks. Tests can then run the real checks without a database.
/// </summary>
/// <remarks>
///     <see cref="AuthorizationService" /> lets the configured bot owner through before any rule, so the user IDs used
///     here must not be the owner's.
/// </remarks>
public sealed class AuthorizationFixture
{
    private const ulong GuildId = 900;
    private const ulong BotId = 901;

    private readonly Dictionary<ulong, GuildUserEntity> _users = new();

    public AuthorizationFixture()
    {
        // The rule AuthorizationService.AutoConfigureGuild seeds, so it never tries to save.
        Guild.AuthorizationGroups.Add(new AuthorizationGroup(
            AuthorizationScope.All, AccessType.Allow, JudgeType.Any,
            new PermissionCriterion(GuildPermission.Administrator)));

        var guilds = new Mock<DbSet<GuildEntity>>();
        guilds
            .Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask<GuildEntity?>(Guild));

        var users = new Mock<DbSet<GuildUserEntity>>();
        users
            .Setup(s => s.FindAsync(It.IsAny<object?[]?>(), It.IsAny<CancellationToken>()))
            .Returns((object?[]? keys, CancellationToken _) => new ValueTask<GuildUserEntity?>(Entity((ulong) keys![0]!)));

        Db = new HuTaoContext(new DbContextOptionsBuilder<HuTaoContext>().Options)
        {
            Guilds = guilds.Object,
            Users  = users.Object
        };

        Auth = new AuthorizationService(Db);

        var guild = new Mock<IGuild>();
        DiscordGuild = guild.Object;
        guild.SetupGet(g => g.Id).Returns(GuildId);
        guild
            .Setup(g => g.GetCurrentUserAsync(It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
            .ReturnsAsync(() => User(BotId));
    }

    public GuildEntity Guild { get; } = new(GuildId);

    public HuTaoContext Db { get; }

    public AuthorizationService Auth { get; }

    public IGuild DiscordGuild { get; }

    public IServiceProvider Services
    {
        get
        {
            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(typeof(HuTaoContext))).Returns(Db);
            services.Setup(s => s.GetService(typeof(AuthorizationService))).Returns(Auth);
            return services.Object;
        }
    }

    public ModerationCategory AddCategory(string name)
    {
        var category = new ModerationCategory(name, null, null) { Id = Guid.NewGuid() };
        Guild.ModerationCategories.Add(category);
        return category;
    }

    /// <summary>Adds a rule that allows <paramref name="user" /> the <paramref name="scope" />.</summary>
    public static void Allow(ICollection<AuthorizationGroup> rules, AuthorizationScope scope, IUser user)
        => rules.Add(new AuthorizationGroup(scope, AccessType.Allow, JudgeType.Any, new UserCriterion(user.Id)));

    /// <summary>A member of the guild with no permissions or roles.</summary>
    public IGuildUser User(ulong id, ModerationCategory? defaultCategory = null)
    {
        Entity(id).DefaultCategory = defaultCategory;

        var user = new Mock<IGuildUser>();
        user.SetupGet(u => u.Id).Returns(id);
        user.SetupGet(u => u.Guild).Returns(DiscordGuild);
        user.SetupGet(u => u.GuildId).Returns(GuildId);
        user.SetupGet(u => u.RoleIds).Returns([]);
        user.SetupGet(u => u.GuildPermissions).Returns(GuildPermissions.None);
        return user.Object;
    }

    public IInteractionContext Interaction(IGuildUser user)
    {
        var context = new Mock<IInteractionContext>();
        context.SetupGet(c => c.Guild).Returns(DiscordGuild);
        context.SetupGet(c => c.User).Returns(user);
        context.SetupGet(c => c.Channel).Returns(Channel());
        return context.Object;
    }

    public ICommandContext Command(IGuildUser user)
    {
        var context = new Mock<ICommandContext>();
        context.SetupGet(c => c.Guild).Returns(DiscordGuild);
        context.SetupGet(c => c.User).Returns(user);
        context.SetupGet(c => c.Channel).Returns(Channel());
        return context.Object;
    }

    // Criteria only judge users in a guild channel.
    private static ITextChannel Channel() => Mock.Of<ITextChannel>(c => c.Id == 902);

    private GuildUserEntity Entity(ulong id)
    {
        if (!_users.TryGetValue(id, out var entity))
            _users[id] = entity = new GuildUserEntity(id, GuildId);

        return entity;
    }
}
