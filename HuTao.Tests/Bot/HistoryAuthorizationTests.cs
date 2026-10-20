using System;
using System.Linq;
using System.Reflection;
using HuTao.Bot.Modules;
using HuTao.Bot.Modules.Moderation;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Core.Preconditions.Interactions;
using Xunit;
using Commands = HuTao.Services.Core.Preconditions.Commands;

namespace HuTao.Tests.Bot;

/// <summary>
///     The default-category precondition refuses a moderator whose rights come from a category other than their
///     default. History opens on a category the requester may view, so its handlers accept History from any category.
/// </summary>
public class HistoryAuthorizationTests
{
    private const BindingFlags Members
        = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    [Theory]
    [InlineData(typeof(InteractiveReprimandsModule), nameof(InteractiveReprimandsModule.ViewUserHistoryAsync))]
    [InlineData(typeof(InteractiveUserModule), nameof(InteractiveUserModule.UserHistoryAsync))]
    [InlineData(typeof(InteractiveUserModule), nameof(InteractiveUserModule.ComponentHistoryAsync))]
    public void HistoryHandler_AcceptsHistoryFromAnyCategory(Type module, string name)
    {
        var method = module.GetMethod(name, Members);
        Assert.NotNull(method);

        var precondition = method.GetCustomAttribute<RequireAnyCategoryAuthorizationAttribute>();
        Assert.NotNull(precondition);

        var field = typeof(RequireAnyCategoryAuthorizationAttribute)
            .GetField("_scopes", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        Assert.Equal(AuthorizationScope.All | AuthorizationScope.History, field.GetValue(precondition));
    }

    [Fact]
    public void NoHandler_UsesTheDefaultCategoryPrecondition()
    {
        var types = typeof(HelpModule).Assembly.DefinedTypes.ToList();
        var members = types
            .Cast<MemberInfo>()
            .Concat(types.SelectMany(t => t.GetMethods(Members)))
            .Where(m => m.IsDefined(typeof(RequireCategoryAuthorizationAttribute), true)
                || m.IsDefined(typeof(Commands.RequireCategoryAuthorizationAttribute), true))
            .Select(m => m is Type t ? t.Name : $"{m.DeclaringType?.Name}.{m.Name}")
            .ToList();

        Assert.True(members.Count == 0,
            $"Handlers still guarded by the default-category precondition:{Environment.NewLine}"
            + string.Join(Environment.NewLine, members));
    }
}
