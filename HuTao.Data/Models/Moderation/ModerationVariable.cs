using System;

namespace HuTao.Data.Models.Moderation;

public class ModerationVariable
{
    protected ModerationVariable() { }

    public ModerationVariable(string name, string value)
    {
        Name  = name;
        Value = value;
    }

    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Value { get; set; } = null!;
}