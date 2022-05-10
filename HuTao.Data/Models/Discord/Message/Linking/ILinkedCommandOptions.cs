using System;
using Discord;
using HuTao.Data.Models.Criteria;

namespace HuTao.Data.Models.Discord.Message.Linking;

public interface ILinkedCommandOptions : IMessageTemplateOptions, IRoleTemplateOptions, ICriteriaOptions
{
    public bool Ephemeral { get; }

    public bool Silent { get; }

    public IMessage? Message { get; }

    public string? Description { get; }

    public TimeSpan? Cooldown { get; }

    public UserTargetOptions UserOptions { get; }
}