using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Data.Models.Moderation.Infractions.Actions;

public class KickAction : ReprimandAction, IKick
{
    public override string Action => nameof(Kick);
}