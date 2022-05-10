﻿using HuTao.Services.Core.Messages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.AutoRemoveMessage;

/// <summary>
///     Contains extension methods for configuring the auto remove message feature upon application startup.
/// </summary>
public static class AutoRemoveMessageSetup
{
    /// <summary>
    ///     Adds the services and classes that make up the auto remove message service to a service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the auto remove message services are to be added.</param>
    /// <returns>
    ///     <paramref name="services" />
    /// </returns>
    public static IServiceCollection AddAutoRemoveMessage(this IServiceCollection services)
        => services
            .AddScoped<IRemovableMessageService, RemovableMessageService>()
            .AddScoped<INotificationHandler<ReactionAddedNotification>, AutoRemoveMessageHandler>()
            .AddScoped<INotificationHandler<RemovableMessageRemovedNotification>, AutoRemoveMessageHandler>()
            .AddScoped<INotificationHandler<RemovableMessageSentNotification>, AutoRemoveMessageHandler>();
}