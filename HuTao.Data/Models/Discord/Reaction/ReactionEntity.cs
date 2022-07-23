using System;
using Discord;

namespace HuTao.Data.Models.Discord.Reaction;

public abstract class ReactionEntity : IEmote, IEquatable<IEmote>
{
    protected ReactionEntity() { }

    protected ReactionEntity(IEmote emote) { Name = emote.Name; }

    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public bool Equals(IEmote? other) => Name == other?.Name;
}