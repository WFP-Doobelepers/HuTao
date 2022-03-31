using System;
using Discord;
using Zhongli.Data.Models.Authorization;
using Zhongli.Data.Models.Criteria;

namespace Zhongli.Data.Models.Discord.Message.Linking;

public interface ILinkedCommandOptions : IMessageTemplateOptions, IRoleTemplateOptions, ICriteriaOptions
{
    public AuthorizationScope Scope { get; }

    public bool Ephemeral { get; }

    public bool Silent { get; set; }

    public IMessage? Message { get; }

    public string? Description { get; }

    public TimeSpan? Cooldown { get; }

    public UserTargetOptions UserOptions { get; }
}