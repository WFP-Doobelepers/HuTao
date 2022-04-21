using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Data.Models.Moderation.Infractions;

public interface INotice : IAction
{
    string IAction.Action => nameof(Notice);

    string IAction.CleanAction => nameof(Notice);
}