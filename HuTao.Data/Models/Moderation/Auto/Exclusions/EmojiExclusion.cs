using System.Diagnostics.CodeAnalysis;
using Discord;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Discord.Reaction;
using HuTao.Data.Models.Moderation.Auto.Configurations;

namespace HuTao.Data.Models.Moderation.Auto.Exclusions;

public class EmojiExclusion : ModerationExclusion, IJudge<IEmote>, IJudge<Emoji>, IJudge<Emote>, IJudge<string>
{
    protected EmojiExclusion() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public EmojiExclusion(ReactionEntity emoji, AutoConfiguration? config) : base(config) { Emoji = emoji; }

    public virtual ReactionEntity Emoji { get; set; } = null!;

    public bool Judge(Emoji emoji) => Judge(emoji.Name);

    public bool Judge(Emote emote) => Judge(emote.Name);

    public bool Judge(IEmote emote) => Judge(emote.Name);

    public bool Judge(string judge) => Emoji.Name == judge;
}