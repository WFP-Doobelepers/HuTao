using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Data.Models.Moderation.Infractions.Actions;

public class NoticeAction : ReprimandAction, INotice
{
    public override string Action => nameof(Notice);
}