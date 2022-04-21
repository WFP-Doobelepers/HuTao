using System;
using System.ComponentModel;

namespace HuTao.Data.Models.Discord.Message.Linking;

[Flags]
public enum UserTargetOptions
{
    None = 0,

    [Description("DM the users when the command runs.")]
    DmUser = 1 << 0,

    [Description("Apply the command to yourself.")]
    ApplySelf = 1 << 1,

    [Description("Apply the command to the mentioned users.")]
    ApplyMentions = 1 << 2
}