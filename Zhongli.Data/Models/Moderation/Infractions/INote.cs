using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Data.Models.Moderation.Infractions;

public interface INote : IAction
{
    string IAction.Action => nameof(Note);
}