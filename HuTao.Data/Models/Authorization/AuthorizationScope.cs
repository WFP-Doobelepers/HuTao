using System;
using System.ComponentModel;

namespace HuTao.Data.Models.Authorization;

[Flags]
public enum AuthorizationScope
{
    None = 0,

    [Description("All permissions. Dangerous!")]
    All = 1 << 0,
    Warning = 1 << 1,
    Mute = 1 << 2,
    Kick = 1 << 3,
    Ban = 1 << 4,

    [Description("Allows configuration of settings.")]
    Configuration = 1 << 5,

    [Description("Allows using the quote feature.")]
    Quote = 1 << 6,
    Note = 1 << 7,

    [Description("Allows managing of roles.")]
    ManageRoles = 1 << 8,

    [Description("Allows usage of the user module.")]
    User = 1 << 9,

    [Description("Allows usage of the purge module.")]
    Purge = 1 << 10,

    [Description("Allows managing of channels.")]
    Channels = 1 << 11,

    [Description("Allows using ephemeral messages.")]
    Ephemeral = 1 << 12,

    [Description("Allows viewing of the moderation log.")]
    History = 1 << 13,

    [Description("Allow updating the moderation log.")]
    Modify = 1 << 14,

    [Description("Allow using the slowmode command.")]
    Slowmode = 1 << 15,

    [Description("Allows using the say command.")]
    Send = 1 << 16,

    [Description("Allows using the role module.")]
    Roles = 1 << 17
}