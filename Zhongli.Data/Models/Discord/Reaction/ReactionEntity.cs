using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Reaction;

public abstract class ReactionEntity : IEmote
{
    protected ReactionEntity() { }

    protected ReactionEntity(IEmote emote) { Name = emote.Name; }

    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
}