using System;
using Discord;

namespace HuTao.Data.Models.Discord.Message.Embeds;

public class Field : IEquatable<Field>
{
    protected Field() { }

    public Field(EmbedField field)
    {
        Inline = field.Inline;
        Name   = field.Name;
        Value  = field.Value;
    }

    public Guid Id { get; init; }

    /// <inheritdoc cref="EmbedField.Inline" />
    public bool Inline { get; init; }

    /// <inheritdoc cref="EmbedField.Name" />
    public string Name { get; init; } = null!;

    /// <inheritdoc cref="EmbedField.Value" />
    public string Value { get; init; } = null!;

    /// <inheritdoc />
    public bool Equals(Field? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Inline == other.Inline && Name == other.Name && Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || (obj is Field other && Equals(other));

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Inline, Name, Value);

    public static implicit operator Field(EmbedField field) => new(field);
}