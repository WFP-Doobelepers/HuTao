using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Commands;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Moderation;
using HuTao.Bot.Modules;
using HuTao.Bot.Modules.Configuration;
using HuTao.Bot.Modules.Linking;
using HuTao.Bot.Modules.Moderation;
using HuTao.Services.Core;
using HuTao.Services.Moderation;
using HuTao.Services.VoiceChat;
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

    public static TheoryData<Type, string> MuteRoleMethods => new()
    {
        { typeof(ConfigureModule), nameof(ConfigureModule.ConfigureMuteAsync) },
        { typeof(ConfigureModule), nameof(ConfigureModule.ConfigureHardMuteAsync) },
        { typeof(InteractiveConfigurationModule), nameof(InteractiveConfigurationModule.SelectRoleAsync) }
    };

    // Every command that adds or removes an authorization rule, in the server or in a moderation category.
    public static TheoryData<Type, string, Type[]> RuleChangingMethods => new()
    {
        { typeof(PermissionsModule), nameof(PermissionsModule.AddPermissionAsync), [typeof(AuthorizationScope), typeof(PermissionsModule.RuleOptions)] },
        { typeof(PermissionsModule), nameof(PermissionsModule.InteractiveConfigureAsync), [] },
        { typeof(PermissionsModule), "RemoveEntityAsync", [typeof(string)] },
        { typeof(CategoryPermissionsModule), nameof(CategoryPermissionsModule.AddPermissionAsync), [typeof(ModerationCategory), typeof(AuthorizationScope), typeof(CategoryPermissionsModule.RuleOptions)] },
        { typeof(CategoryPermissionsModule), "RemoveEntityAsync", [typeof(string)] },
        { typeof(InteractivePermissionsModule), nameof(InteractivePermissionsModule.AddRolePermissionAsync), [typeof(AuthorizationScope), typeof(IRole), typeof(AccessType), typeof(bool)] },
        { typeof(InteractivePermissionsModule), nameof(InteractivePermissionsModule.AddUserPermissionAsync), [typeof(AuthorizationScope), typeof(IUser), typeof(AccessType), typeof(bool)] },
        { typeof(InteractivePermissionsModule), nameof(InteractivePermissionsModule.AddChannelPermissionAsync), [typeof(AuthorizationScope), typeof(IGuildChannel), typeof(AccessType), typeof(bool)] },
        { typeof(InteractivePermissionsModule), "RemoveEntityAsync", [typeof(string), typeof(bool)] },
        { typeof(InteractiveCategoryPermissionsModule), nameof(InteractiveCategoryPermissionsModule.AddRolePermissionAsync), [typeof(ModerationCategory), typeof(AuthorizationScope), typeof(IRole), typeof(AccessType), typeof(bool)] },
        { typeof(InteractiveCategoryPermissionsModule), nameof(InteractiveCategoryPermissionsModule.AddUserPermissionAsync), [typeof(ModerationCategory), typeof(AuthorizationScope), typeof(IUser), typeof(AccessType), typeof(bool)] },
        { typeof(InteractiveCategoryPermissionsModule), nameof(InteractiveCategoryPermissionsModule.AddChannelPermissionAsync), [typeof(ModerationCategory), typeof(AuthorizationScope), typeof(IGuildChannel), typeof(AccessType), typeof(bool)] },
        { typeof(InteractiveCategoryPermissionsModule), nameof(InteractiveCategoryPermissionsModule.AddGuildPermissionAsync), [typeof(ModerationCategory), typeof(AuthorizationScope), typeof(HuTao.Data.Models.Discord.GuildPermission), typeof(AccessType), typeof(bool)] },
        { typeof(InteractiveCategoryPermissionsModule), nameof(InteractiveCategoryPermissionsModule.RemovePermissionAsync), [typeof(string), typeof(bool)] }
    };

    public static TheoryData<Type, string, Type> OtherGuardedMethods => new()
    {
        { typeof(ModerationModule), "GetDetailsAsync", typeof(ReprimandCategories) },
        { typeof(InteractiveModerationModule), "GetDetailsAsync", typeof(ReprimandCategories) },
        { typeof(ChannelModule), nameof(ChannelModule.SyncPermissionsAsync), typeof(ChannelSyncAccess) },
        { typeof(ChannelModule), nameof(ChannelModule.SyncCategoryPermissionsAsync), typeof(ChannelSyncAccess) },
        { typeof(VoiceChatModule), nameof(VoiceChatModule.HideAsync), typeof(VoiceChatOwnership) },
        { typeof(VoiceChatModule), nameof(VoiceChatModule.LockAsync), typeof(VoiceChatOwnership) },
        { typeof(VoiceChatModule), nameof(VoiceChatModule.RevealAsync), typeof(VoiceChatOwnership) },
        { typeof(VoiceChatModule), nameof(VoiceChatModule.UnlockAsync), typeof(VoiceChatOwnership) },
        { typeof(VoiceChatModule), nameof(VoiceChatModule.TransferAsync), typeof(VoiceChatOwnership) },
        { typeof(InteractiveVoiceChatModule), "CanManageAsync", typeof(VoiceChatOwnership) }
    };

    [Theory]
    [MemberData(nameof(GuardedMethods))]
    public void RoleGrantingMethod_ChecksTheInvokerAgainstRoleHierarchy(Type module, string method)
    {
        var info = module.GetMethod(method, All);

        Assert.NotNull(info);
        Assert.True(Calls(info, m => m.DeclaringType == typeof(RoleHierarchy), []),
            $"{module.Name}.{method} never calls {nameof(RoleHierarchy)}.");
    }

    [Theory]
    [MemberData(nameof(MuteRoleMethods))]
    public void MuteRoleMethod_RefusesEveryoneManagedAndBotRoles(Type module, string method)
    {
        var info = module.GetMethod(method, All);

        Assert.NotNull(info);
        Assert.True(Calls(info, m => m.DeclaringType == typeof(RoleHierarchy) && m.Name == nameof(RoleHierarchy.CheckMuteRole), []),
            $"{module.Name}.{method} never calls {nameof(RoleHierarchy.CheckMuteRole)}.");
    }

    [Theory]
    [MemberData(nameof(OtherGuardedMethods))]
    public void MethodThatActsWithTheBotsAuthority_ChecksTheMemberFirst(Type module, string method, Type check)
    {
        var info = module.GetMethod(method, All);

        Assert.NotNull(info);
        Assert.True(Calls(info, m => m.DeclaringType == check, []), $"{module.Name}.{method} never calls {check.Name}.");
    }

    [Theory]
    [MemberData(nameof(RuleChangingMethods))]
    public void MethodThatAddsOrRemovesARule_ChecksTheModeratorFirst(Type module, string method, Type[] parameters)
    {
        var info = module.GetMethod(method, All, parameters);

        Assert.NotNull(info);
        Assert.True(
            Calls(info, m => m.DeclaringType == typeof(AuthorizationService)
                && m.Name == nameof(AuthorizationService.RefuseRuleChangeAsync), []),
            $"{module.Name}.{method} never calls {nameof(AuthorizationService.RefuseRuleChangeAsync)}.");
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

    private static bool Calls(MethodInfo method, Func<MethodBase, bool> isCheck, HashSet<MethodBase> visited)
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

            if (target is not null && isCheck(target)) return true;

            // Helpers on the same module, such as a private permission check shared by two commands.
            if (target is MethodInfo helper && target.DeclaringType == method.DeclaringType
                && Calls(helper, isCheck, visited))
                return true;
        }

        return false;
    }
}
