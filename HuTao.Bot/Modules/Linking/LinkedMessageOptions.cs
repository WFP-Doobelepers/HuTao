using System.Collections.Generic;
using Discord;
using Discord.Commands;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.CommandHelp;

namespace HuTao.Bot.Modules.Linking;

[NamedArgumentType]
public class LinkedMessageOptions : ILinkedButtonOptions
{
    [HelpSummary("Optionally provide a channel to send the new message to.")]
    public ITextChannel? Channel { get; set; }

    [HelpSummary("DM the user if the Button is clicked.")]
    public bool DmUser { get; set; }

    [HelpSummary("`True` to send the message as ephemeral and `False` to not.")]
    public bool Ephemeral { get; set; }

    [HelpSummary("`True` if you want the Button to be disabled, `False` if not.")]
    public bool IsDisabled { get; set; }

    [HelpSummary("The style of the Button.")]
    public ButtonStyle Style { get; set; } = ButtonStyle.Primary;

    [HelpSummary($"The {nameof(IEmote)} to be displayed with the Button.")]
    public IEmote? Emote { get; set; }

    [HelpSummary("The message to use as a template for the message link.")]
    public IMessage? Message { get; set; }

    [HelpSummary("The row where the button should be placed.")]
    public int Row { get; set; }

    [HelpSummary("The text label that is shown on the Button.")]
    public string? Label { get; set; }

    [HelpSummary("The URL of the Button.")]
    public string? Url { get; set; }

    [HelpSummary("`True` to allow mentions and `False` to not.")]
    public bool AllowMentions { get; set; }

    [HelpSummary("`True` if you want the message to be live, where it will update its contents continuously.")]
    public bool IsLive { get; set; }

    [HelpSummary("`True` if you want embed timestamps to use the current time, `False` if not.")]
    public bool ReplaceTimestamps { get; set; }

    [HelpSummary("`True` if you want embeds to be suppressed, `False` if not.")]
    public bool SuppressEmbeds { get; set; }

    [HelpSummary("The roles to be added when this button is pressed.")]
    public IEnumerable<IRole>? AddRoles { get; set; }

    [HelpSummary("The roles to be removed when this button is pressed.")]
    public IEnumerable<IRole>? RemoveRoles { get; set; }

    [HelpSummary("The roles to be toggled when this button is pressed.")]
    public IEnumerable<IRole>? ToggleRoles { get; set; }
}