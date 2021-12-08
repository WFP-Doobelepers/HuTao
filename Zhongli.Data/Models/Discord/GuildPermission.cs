using System;

namespace Zhongli.Data.Models.Discord;

[Flags]
public enum GuildPermission
{
    None = 0,

    /// <summary>Allows kicking members.</summary>
    /// <remarks>
    ///     This permission requires the owner account to use two-factor
    ///     authentication when used on a guild that has server-wide 2FA enabled.
    /// </remarks>
    KickMembers = 2,

    /// <summary>Allows banning members.</summary>
    /// <remarks>
    ///     This permission requires the owner account to use two-factor
    ///     authentication when used on a guild that has server-wide 2FA enabled.
    /// </remarks>
    BanMembers = 4,

    /// <summary>
    ///     Allows all permissions and bypasses channel permission overwrites.
    /// </summary>
    /// <remarks>
    ///     This permission requires the owner account to use two-factor
    ///     authentication when used on a guild that has server-wide 2FA enabled.
    /// </remarks>
    Administrator = 8,

    /// <summary>Allows management and editing of channels.</summary>
    /// <remarks>
    ///     This permission requires the owner account to use two-factor
    ///     authentication when used on a guild that has server-wide 2FA enabled.
    /// </remarks>
    ManageChannels = 16, // 0x0000000000000010

    /// <summary>Allows management and editing of the guild.</summary>
    /// <remarks>
    ///     This permission requires the owner account to use two-factor
    ///     authentication when used on a guild that has server-wide 2FA enabled.
    /// </remarks>
    ManageGuild = 32, // 0x0000000000000020

    /// <summary>Allows for deletion of other users messages.</summary>
    /// <remarks>
    ///     This permission requires the owner account to use two-factor
    ///     authentication when used on a guild that has server-wide 2FA enabled.
    /// </remarks>
    ManageMessages = 8192, // 0x0000000000002000

    /// <summary>Allows for muting members in a voice channel.</summary>
    MuteMembers = 4194304, // 0x0000000000400000

    /// <summary>
    ///     Allows for deafening of members in a voice channel.
    /// </summary>
    DeafenMembers = 8388608, // 0x0000000000800000

    /// <summary>
    ///     Allows for moving of members between voice channels.
    /// </summary>
    MoveMembers = 16777216, // 0x0000000001000000

    /// <summary>Allows for modification of other users nicknames.</summary>
    ManageNicknames = 134217728, // 0x0000000008000000

    /// <summary>Allows management and editing of roles.</summary>
    /// <remarks>
    ///     This permission requires the owner account to use two-factor
    ///     authentication when used on a guild that has server-wide 2FA enabled.
    /// </remarks>
    ManageRoles = 268435456, // 0x0000000010000000

    /// <summary>Allows management and editing of webhooks.</summary>
    /// <remarks>
    ///     This permission requires the owner account to use two-factor
    ///     authentication when used on a guild that has server-wide 2FA enabled.
    /// </remarks>
    ManageWebhooks = 536870912, // 0x0000000020000000

    /// <summary>Allows management and editing of emojis.</summary>
    /// <remarks>
    ///     This permission requires the owner account to use two-factor
    ///     authentication when used on a guild that has server-wide 2FA enabled.
    /// </remarks>
    ManageEmojis = 1073741824 // 0x0000000040000000
}