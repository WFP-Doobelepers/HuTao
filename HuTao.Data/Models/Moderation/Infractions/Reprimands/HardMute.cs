using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Discord;
using Humanizer;
using HuTao.Data.Models.Discord;

namespace HuTao.Data.Models.Moderation.Infractions.Reprimands;

public class HardMute : Mute, IHardMute
{
    protected HardMute() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public HardMute(TimeSpan? length, ICollection<RoleEntity> roles, ReprimandDetails details) : base(length, details)
    {
        Roles = roles;
    }

    public virtual ICollection<RoleEntity> Roles { get; set; } = new List<RoleEntity>();

    string IAction.Action => $"Hard mute {Format.Bold(Length?.Humanize() ?? "indefinitely")}";

    string IAction.CleanAction => $"Hard mute {Length?.Humanize() ?? "indefinitely"}";
}