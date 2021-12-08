namespace Zhongli.Data.Models.Moderation.Infractions.Reprimands;

public class Kick : Reprimand, IKick
{
    protected Kick() { }

    public Kick(ReprimandDetails details) : base(details) { }
}