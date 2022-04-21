using HuTao.Data.Models.Moderation.Infractions.Reprimands;

namespace HuTao.Data.Models.Moderation.Infractions;

public interface IKick : IAction
{
    string IAction.Action => nameof(Kick);

    string IAction.CleanAction => nameof(Kick);
}