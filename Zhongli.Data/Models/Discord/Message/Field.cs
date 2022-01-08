using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Field
{
    protected Field() { }

    public Field(EmbedField field)
    {
        Inline = field.Inline;
        Name   = field.Name;
        Value  = field.Value;
    }

    public Guid Id { get; set; }

    /// <inheritdoc cref="EmbedField.Inline" />
    public bool Inline { get; set; }

    /// <inheritdoc cref="EmbedField.Name" />
    public string Name { get; set; }

    /// <inheritdoc cref="EmbedField.Value" />
    public string Value { get; set; }
}