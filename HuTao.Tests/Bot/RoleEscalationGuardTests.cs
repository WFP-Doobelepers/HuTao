using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Commands;
using HuTao.Bot.Modules;
using HuTao.Bot.Modules.Configuration;
using HuTao.Bot.Modules.Linking;
using HuTao.Bot.Modules.Moderation;
using HuTao.Services.Core;
using Xunit;

namespace HuTao.Tests.Bot;

/// <summary>
///     The bot gives and takes roles with its own permissions, so every command or panel that saves a role a member chose,
///     or creates a role for a member, must hold that member to the roles and permissions they have themselves. The
///     modules need a socket context that cannot be built in a test, so these tests check that each such method calls
///     <see cref="RoleHierarchy" />, and that no new role-granting option type appears without being listed here.
/// </summary>
public class RoleEscalationGuardTests
{
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly string[] GrantProperties = ["AddRoles", "RemoveRoles", "ToggleRoles"];

    public static TheoryData<Type, string> GuardedMethods => new()
    {
        { typeof(LinkedButtonModule), nameof(LinkedButtonModule.LinkAsync) },
        { typeof(MessageTemplateModule), nameof(MessageTemplateModule.LinkAsync) },
        { typeof(LinkedCommandModule), nameof(LinkedCommandModule.LinkAsync) },
        { typeof(ModerationTemplatesModule), nameof(ModerationTemplatesModule.RoleTemplateAsync) },
        { typeof(ReprimandTriggersModule), nameof(ReprimandTriggersModule.RoleTriggerAsync) },
        { typeof(ConfigureModule), nameof(ConfigureModule.ConfigureMuteAsync) },
        { typeof(ConfigureModule), nameof(ConfigureModule.ConfigureHardMuteAsync) },
        { typeof(RoleModule.AuthorizedRoleModule), nameof(RoleModule.AuthorizedRoleModule.CreateRoleAsync) },
        { typeof(RoleModule.AuthorizedRoleModule), nameof(RoleModule.AuthorizedRoleModule.TemporaryRoleCreateAsync) },
        { typeof(InteractiveConfigurationModule), nameof(InteractiveConfigurationModule.SelectRoleAsync) }
    };

    [Theory]
    [MemberData(nameof(GuardedMethods))]
    public void RoleGrantingMethod_ChecksTheInvokerAgainstRoleHierarchy(Type module, string method)
    {
        var info = module.GetMethod(method, All);

        Assert.NotNull(info);
        Assert.True(CallsRoleHierarchy(info, []), $"{module.Name}.{method} never calls {nameof(RoleHierarchy)}.");
    }

    [Fact]
    public void RoleGrantingOptionTypes_AreAllCoveredByAGuardedCommand()
    {
        var optionTypes = typeof(RoleModule).Assembly.GetTypes()
            .Where(t => GrantProperties.All(p => t.GetProperty(p)?.PropertyType == typeof(IEnumerable<IRole>)))
            .Select(t => t.Name)
            .Order()
            .ToList();

        Assert.Equal(
            ["LinkedCommandOptions", "LinkedMessageOptions", "RoleReprimandOptions", "RoleTemplateOptions"],
            optionTypes);
    }

    private static bool CallsRoleHierarchy(MethodInfo method, HashSet<MethodBase> visited)
    {
        if (!visited.Add(method)) return false;

        var body = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
            .GetMethod("MoveNext", All) ?? method;

        var il = body.GetMethodBody()?.GetILAsByteArray();
        if (il is null) return false;

        for (var i = 0; i + 4 < il.Length; i++)
        {
            // call (0x28) and callvirt (0x6F), followed by the target's metadata token.
            if (il[i] is not (0x28 or 0x6F)) continue;

            MethodBase? target;
            try
            {
                target = body.Module.ResolveMethod(BitConverter.ToInt32(il, i + 1),
                    body.DeclaringType?.IsGenericType == true ? body.DeclaringType.GetGenericArguments() : null,
                    body.IsGenericMethod ? body.GetGenericArguments() : null);
            }
            catch (Exception e) when (e is ArgumentException or BadImageFormatException)
            {
                continue;
            }

            if (target?.DeclaringType == typeof(RoleHierarchy)) return true;

            // Helpers on the same module, such as a private permission check shared by two commands.
            if (target is MethodInfo helper && target.DeclaringType == method.DeclaringType
                && CallsRoleHierarchy(helper, visited))
                return true;
        }

        return false;
    }
}
