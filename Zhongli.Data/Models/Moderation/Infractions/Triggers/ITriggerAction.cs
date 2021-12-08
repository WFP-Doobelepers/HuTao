using System;
using Zhongli.Data.Models.Moderation.Infractions.Actions;

namespace Zhongli.Data.Models.Moderation.Infractions.Triggers;

public interface ITriggerAction
{
    Guid? ReprimandId { get; set; }

    ReprimandAction? Reprimand { get; set; }
}