using System;
using Discord;

namespace HuTao.Services.Utilities;

public static class MessageUpdateUtilities
{
    /// <summary>
    ///     Determines whether a Discord "message updated" gateway event represents a real user edit that changed the text
    ///     content. Discord may emit update events for non-user changes (e.g., embed refreshes, pin/unpin), which should not
    ///     trigger moderation actions or message edit logs.
    /// </summary>
    public static bool IsUserContentEdit(IMessage newMessage, IMessage? oldMessage = null)
    {
        if (newMessage is not IUserMessage { EditedTimestamp: not null } newUserMessage)
            return false;

        if (oldMessage is IUserMessage oldUserMessage
            && string.Equals(oldUserMessage.Content, newUserMessage.Content, StringComparison.Ordinal))
            return false;

        return true;
    }
}

