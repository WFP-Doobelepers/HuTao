using System;
using System.ComponentModel;

namespace Zhongli.Data.Models.Moderation
{
    [Flags]
    public enum ReprimandOptions
    {
        None = 0,

        [Description("Separate reprimands and output verbosely.")]
        Verbose = 1 << 0,

        [Description("Silently reprimand user by deleting the message.")]
        Silent = 1 << 1,

        [Description("Notifies the user by DMing them.")]
        NotifyUser = 1 << 2,

        [Description("When notifying a user, make the moderator anonymous.")]
        Anonymous = 1 << 3
    }
}