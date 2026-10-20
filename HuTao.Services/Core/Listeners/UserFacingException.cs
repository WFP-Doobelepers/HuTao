using System;

namespace HuTao.Services.Core.Listeners;

/// <summary>
///     A failure that a command reports on purpose, such as a channel that is already at the top of its category. Its
///     message is written for the person who ran the command, so it is shown to them. Any other exception shows only an
///     error ID.
/// </summary>
public class UserFacingException(string message) : InvalidOperationException(message);
