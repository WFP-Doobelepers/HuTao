using System;
using System.Diagnostics.CodeAnalysis;
using Zhongli.Data.Models.Moderation.Infractions;

namespace Zhongli.Data.Models.Logging;

public abstract class DeleteLog : ILog, IModerationAction
{
    protected DeleteLog() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected DeleteLog(ActionDetails? details)
    {
        LogDate = DateTimeOffset.UtcNow;
        Action  = details?.ToModerationAction();
    }

    public Guid Id { get; set; }

    public DateTimeOffset LogDate { get; set; }

    public virtual ModerationAction? Action { get; set; }
}