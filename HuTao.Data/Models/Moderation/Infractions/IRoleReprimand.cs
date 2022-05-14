using System.Collections.Generic;
using System.Linq;
using Discord;
using Humanizer;
using HuTao.Data.Models.Discord;
using HuTao.Data.Models.Discord.Message.Linking;

namespace HuTao.Data.Models.Moderation.Infractions;

public interface IRoleReprimand : IAction, ILength
{
    public Dictionary<RoleBehavior, List<RoleTemplate>> Actions
        => Roles.GroupBy(r => r.Behavior).ToDictionary(g => g.Key, g => g.ToList());

    public ICollection<RoleTemplate> Roles { get; set; }

    public string Humanized => Actions
        .Where(a => a.Value.Any())
        .Humanize(a => $"{a.Key} {a.Value.Humanize(r => r.MentionRole())}");

    string IAction.Action => $"{Humanized} {Format.Bold(Length?.Humanize() ?? "indefinitely")}";

    string IAction.CleanAction => $"{Humanized} {Length?.Humanize() ?? "indefinitely"}";
}