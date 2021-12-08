using Zhongli.Data.Models.Moderation.Infractions.Reprimands;

namespace Zhongli.Data.Models.Moderation.Infractions.Actions;

public class NoteAction : ReprimandAction, INote
{
    public override string Action => nameof(Note);
}