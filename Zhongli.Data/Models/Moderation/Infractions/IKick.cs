using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Data.Models.Moderation.Infractions;

public interface IKick : IAction
{
    string IAction.Action => nameof(Kick);
}