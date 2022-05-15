using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Data.Models.Moderation.Infractions.Triggers;

namespace HuTao.Bot.Modules.Moderation;

[NamedArgumentType]
public class RoleReprimandOptions : IRoleTemplateOptions, ITrigger
{
    public TimeSpan? Length { get; set; }

    public IEnumerable<IRole>? AddRoles { get; set; }

    public IEnumerable<IRole>? RemoveRoles { get; set; }

    public IEnumerable<IRole>? ToggleRoles { get; set; }

    public ModerationCategory? Category { get; set; }

    public TriggerMode Mode { get; set; } = TriggerMode.Exact;

    public uint Amount { get; set; } = 1;
}