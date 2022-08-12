using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using HuTao.Data;
using HuTao.Data.Models.Moderation.Infractions.Reprimands;
using HuTao.Services.Core;
using HuTao.Services.Core.Messages;
using HuTao.Services.Utilities;
using MediatR;

namespace HuTao.Services.Moderation;

public class SuspiciousAttachmentBehavior : INotificationHandler<MessageReceivedNotification>
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static readonly IReadOnlyCollection<string> BlacklistedExtensions = new[]
    {
        ".7z", ".application", ".bat", ".bin", ".cmd", ".com", ".com", ".cpl", ".dll", ".doc", ".docm", ".dotm", ".exe",
        ".gadget", ".hta", ".inf", ".inf1", ".ins", ".inx", ".isu", ".jar", ".job", ".js", ".jse", ".lnk", ".msc",
        ".msh", ".msh1", ".msh1xml", ".msh2", ".msh2xml", ".mshxml", ".msi", ".msp", ".paf", ".pasc2", ".pdb", ".pif",
        ".potm", ".ppam", ".ppsm", ".ppt", ".pptm", ".ps1", ".ps1xml", ".ps2", ".ps2xml", ".psc1", ".rar", ".reg",
        ".rgs", ".sb", ".scf", ".scr", ".sct", ".sh", ".shb", ".shs", ".sldn", ".u3p", ".vbe", ".vbs", ".vbscript",
        ".ws", ".wsc", ".wsf", ".wsh", ".xlam", ".xls", ".xlsm", ".xltm", ".zip"
    };

    private readonly HuTaoContext _db;
    private readonly ModerationService _moderation;

    public SuspiciousAttachmentBehavior(ModerationService moderation, HuTaoContext db)
    {
        _moderation = moderation;
        _db         = db;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var message = notification.Message;
        var author = message.Author;

        if (author.IsBot || author.IsWebhook
            || author is not IGuildUser user
            || message.Channel is not ITextChannel channel)
            return;

        var guildEntity = await _db.Guilds.TrackGuildAsync(channel.Guild, cancellationToken);
        if (cancellationToken.IsCancellationRequested || guildEntity.ModerationRules is null)
            return;

        if (guildEntity.ModerationRules.CensorExclusions.Any(e => e.Judge(channel, user)))
            return;

        var blacklisted = message.Attachments
            .Select(attachment => attachment.Filename.ToLower())
            .Where(filename => BlacklistedExtensions.Any(filename.EndsWith))
            .ToArray();

        if (!blacklisted.Any()) return;

        var currentUser = await channel.Guild.GetCurrentUserAsync();
        var reason = new StringBuilder()
            .AppendLine("[Suspicious Files]")
            .AppendLine(blacklisted.Humanize());

        var details = new ReprimandDetails(user, currentUser, reason.ToString());
        var length = guildEntity.ModerationRules.CensoredExpiryLength;

        await _moderation.CensorAsync(message, length, details, cancellationToken);
    }
}