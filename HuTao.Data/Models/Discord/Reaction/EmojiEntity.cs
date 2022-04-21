using Discord;

namespace HuTao.Data.Models.Discord.Reaction;

public class EmojiEntity : ReactionEntity
{
    protected EmojiEntity() { }

    public EmojiEntity(IEmote emote) : base(emote) { }

    public override string ToString() => Name;
}