using System;
using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Infractions.Triggers;

public interface ITriggerAction
{
    Guid? ReprimandId { get; set; }

    ReprimandAction? Reprimand { get; set; }
}