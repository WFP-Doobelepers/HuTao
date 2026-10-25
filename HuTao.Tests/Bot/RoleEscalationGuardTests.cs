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
using HuTao.Bot.Modules.Logging;
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
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteUnmuteAsync), typeof(GuildRecords) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteExtendModalAsync), typeof(GuildRecords) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteDetailsAsync), typeof(GuildRecords) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleTimeoutRemoveAsync), typeof(GuildRecords) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleTimeoutDetailsAsync), typeof(GuildRecords) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleAutoToggleAsync), typeof(GuildRecords) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleAutoDeleteAsync), typeof(GuildRecords) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteUnmuteAsync), typeof(RoleHierarchy) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteExtendModalAsync), typeof(RoleHierarchy) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleTimeoutRemoveAsync), typeof(RoleHierarchy) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteUnmuteAsync), typeof(RunningReprimandAccess) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteExtendModalAsync), typeof(RunningReprimandAccess) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleTimeoutRemoveAsync), typeof(RunningReprimandAccess) },
        { typeof(ModerationModule), "GetDetailsAsync", typeof(ReprimandCategories) },
        { typeof(InteractiveModerationModule), "GetDetailsAsync", typeof(ReprimandCategories) },
        { typeof(ModerationModule), nameof(ModerationModule.TemplateAsync), typeof(TemplateAccess) },
        { typeof(InteractiveModerationModule), nameof(InteractiveModerationModule.TemplateAsync), typeof(TemplateAccess) },
        { typeof(ModerationTemplatesModule), "AddTemplateAsync", typeof(TemplateAccess) },
        { typeof(InteractiveTemplatesModule), "AddTemplateAsync", typeof(TemplateAccess) },
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

    // Handlers whose custom ID carries a record's ID. A member of another server can send any ID, so these look records up
    // only through this server's own collections or GuildRecords, never by ID alone.
    public static TheoryData<Type, string> HandlersThatTakeAnIdFromACustomId => new()
    {
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteUnmuteAsync) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteExtendModalAsync) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleMuteDetailsAsync) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleTimeoutRemoveAsync) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleTimeoutDetailsAsync) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleAutoToggleAsync) },
        { typeof(ModerationInteractionHandlers), nameof(ModerationInteractionHandlers.HandleAutoDeleteAsync) },
        { typeof(InteractiveLoggingModule.InteractiveLoggingExclusionsModule), nameof(InteractiveLoggingModule.InteractiveLoggingExclusionsModule.RemoveAsync) }
    };

    // Channel selects that save the channel. The select's values come from the member's client, so the handler keeps
    // only channels of this server.
    public static TheoryData<Type, string> ChannelSelectsThatSave => new()
    {
        { typeof(InteractiveConfigurationModule), nameof(InteractiveConfigurationModule.SelectChannelAsync) },
        { typeof(InteractiveLoggingModule.InteractiveLoggingExclusionsModule), nameof(InteractiveLoggingModule.InteractiveLoggingExclusionsModule.AddChannelAsync) }
    };

    [Theory]
    [MemberData(nameof(ChannelSelectsThatSave))]
    public void ChannelSelectThatSaves_KeepsOnlyThisServersChannels(Type module, string method)
    {
        var info = module.GetMethod(method, All);

        Assert.NotNull(info);
        Assert.True(Calls(info, m => m.Name == "get_GuildId" && m.DeclaringType == typeof(IGuildChannel), []),
            $"{module.Name}.{method} never compares the channel's server.");
    }

    [Theory]
    [MemberData(nameof(HandlersThatTakeAnIdFromACustomId))]
    public void HandlerThatTakesAnIdFromACustomId_NeverLooksItUpByIdAlone(Type module, string method)
    {
        var info = module.GetMethod(method, All);

        Assert.NotNull(info);
        Assert.False(
            Calls(info, m => m.Name == "FindAsync"
                || m.DeclaringType == typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                && m.Name.StartsWith("FirstOrDefault", StringComparison.Ordinal), []),
            $"{module.Name}.{method} looks a record up by its ID alone.");
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
            // call (0x28) and callvirt (0x6F), followed by the target's metadata token, and ldftn (0xFE 0x06), which
            // loads a lambda's method for a delegate.
            var token = il[i] is 0x28 or 0x6F ? i + 1
                : il[i] == 0xFE && il[i + 1] == 0x06 && i + 5 < il.Length ? i + 2
                : -1;
            if (token < 0) continue;

            MethodBase? target;
            try
            {
                target = body.Module.ResolveMethod(BitConverter.ToInt32(il, token),
                    body.DeclaringType?.IsGenericType == true ? body.DeclaringType.GetGenericArguments() : null,
                    body.IsGenericMethod ? body.GetGenericArguments() : null);
            }
            catch (Exception e) when (e is ArgumentException or BadImageFormatException)
            {
                continue;
            }

            if (target is not null && isCheck(target)) return true;

            // Helpers on the same module, such as a private permission check shared by two commands, and the lambdas
            // that the compiler moves into a class inside the module.
            if (target is MethodInfo helper && IsOnModule(helper, method.DeclaringType)
                && Calls(helper, isCheck, visited))
                return true;
        }

        return false;
    }

    private static bool IsOnModule(MethodInfo helper, Type? module)
    {
        var type = helper.DeclaringType;
        while (type is not null && type != module && type.IsDefined(typeof(CompilerGeneratedAttribute)))
            type = type.DeclaringType;

        return type is not null && type == module;
    }
}
