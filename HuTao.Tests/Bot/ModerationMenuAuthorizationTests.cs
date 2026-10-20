using System;
using System.Linq;
using System.Reflection;
using Discord.Interactions;
using HuTao.Bot.Modules.Moderation;
using HuTao.Data.Models.Authorization;
using HuTao.Services.Core.Preconditions.Interactions;
using Xunit;
using static HuTao.Data.Models.Authorization.AuthorizationScope;

namespace HuTao.Tests.Bot;

/// <summary>
///     The moderation menus only open a modal, and the modal checks the category when it is submitted. They must still
///     refuse members, or anyone could open the moderation modals. The check accepts rights from any category, so a
///     moderator whose rights come from a category can still reach the modal and pick that category there.
/// </summary>
public class ModerationMenuAuthorizationTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Theory]
    [InlineData("ban-menu", Ban)]
    [InlineData("hardmute-menu", HardMute)]
    [InlineData("kick-menu", Kick)]
    [InlineData("mute-menu", Mute)]
    [InlineData("note-menu", Note)]
    [InlineData("notice-menu", Warning)]
    [InlineData("timeout-menu", Timeout)]
    [InlineData("warn-menu", Warning)]
    public void MenuCommand_RequiresTheScopeOfItsModal(string name, AuthorizationScope scope)
    {
        var method = Methods().Single(m => m.GetCustomAttribute<SlashCommandAttribute>()?.Name == name);

        Assert.Equal(All | scope, RequiredScopes(method));
    }

    [Fact]
    public void MenuCommands_AreAllCovered()
    {
        var menus = Methods()
            .Select(m => m.GetCustomAttribute<SlashCommandAttribute>()?.Name)
            .OfType<string>()
            .Where(n => n.EndsWith("-menu", StringComparison.Ordinal))
            .Order()
            .ToList();

        // A new menu must be added to the theory above.
        Assert.Equal(
            new[]
            {
                "ban-menu", "hardmute-menu", "kick-menu", "mute-menu",
                "note-menu", "notice-menu", "timeout-menu", "warn-menu"
            },
            menus);
    }

    [Fact]
    public void ModMenuComponent_RequiresAnyModalScope()
    {
        var method = Methods().Single(m => m.GetCustomAttribute<ComponentInteractionAttribute>()?.CustomId == "mod-menu:*");

        Assert.Equal(All | Ban | HardMute | Kick | Mute | Note | Warning | Timeout, RequiredScopes(method));
    }

    private static MethodInfo[] Methods() => typeof(InteractiveModerationModule).GetMethods(Members);

    private static AuthorizationScope RequiredScopes(MethodInfo method)
    {
        var precondition = method.GetCustomAttribute<RequireAnyCategoryAuthorizationAttribute>();
        Assert.NotNull(precondition);
        Assert.Null(precondition.Group);

        var field = typeof(RequireAnyCategoryAuthorizationAttribute)
            .GetField("_scopes", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        return (AuthorizationScope) field.GetValue(precondition)!;
    }
}
