using HuTao.Data.Models.Moderation.Infractions.Reprimands;

namespace HuTao.Data.Models.Moderation.Infractions;

public interface INote : IAction
{
    string IAction.Action => nameof(Note);

    string IAction.CleanAction => nameof(Note);
}