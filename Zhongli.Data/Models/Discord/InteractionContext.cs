using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Discord;

namespace Zhongli.Data.Models.Discord;

/// <inheritdoc cref="IDiscordInteraction" />
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class InteractionContext : Context, IInteractionContext, IDiscordInteraction
{
    private bool _deferred;
    private bool _followed;

    public InteractionContext(IInteractionContext context)
        : base(context.Client, context.Guild, context.Channel, context.User)
    {
        Interaction = context.Interaction;
    }

    /// <inheritdoc />
    ulong IDiscordInteraction.Id => Interaction.Id;

    /// <inheritdoc />
    ulong IEntity<ulong>.Id => Interaction.Id;

    /// <inheritdoc cref="IDiscordInteraction.DeferAsync" />
    public override async Task DeferAsync(bool ephemeral = false, RequestOptions? options = null)
    {
        _deferred = true;
        await Interaction.DeferAsync(ephemeral, options);
    }

    /// <inheritdoc />
    public Task DeleteOriginalResponseAsync(RequestOptions? options = null)
        => Interaction.DeleteOriginalResponseAsync(options);

    /// <inheritdoc />
    public Task RespondAsync(
        string? text = null, Embed[]? embeds = null, bool isTTS = false, bool ephemeral = false,
        AllowedMentions? allowedMentions = null, MessageComponent? components = null, Embed? embed = null,
        RequestOptions? options = null)
        => Interaction.RespondAsync(
            text, embeds, isTTS, ephemeral,
            allowedMentions, components, embed,
            options);

    /// <inheritdoc />
    public Task RespondWithFilesAsync(
        IEnumerable<FileAttachment> attachments, string? text = null, Embed[]? embeds = null, bool isTTS = false,
        bool ephemeral = false, AllowedMentions? allowedMentions = null, MessageComponent? components = null,
        Embed? embed = null, RequestOptions? options = null)
        => Interaction.RespondWithFilesAsync(
            attachments, text, embeds, isTTS,
            ephemeral, allowedMentions, components,
            embed, options);

    /// <inheritdoc />
    public async Task<IUserMessage> FollowupAsync(
        string? text = null, Embed[]? embeds = null, bool isTTS = false,
        bool ephemeral = false, AllowedMentions? allowedMentions = null, MessageComponent? components = null,
        Embed? embed = null, RequestOptions? options = null)
    {
        _followed = true;
        return await Interaction.FollowupAsync(
            text, embeds, isTTS,
            ephemeral, allowedMentions, components,
            embed, options);
    }

    /// <inheritdoc />
    public Task<IUserMessage> FollowupWithFilesAsync(
        IEnumerable<FileAttachment> attachments, string? text = null, Embed[]? embeds = null, bool isTTS = false,
        bool ephemeral = false, AllowedMentions? allowedMentions = null, MessageComponent? components = null,
        Embed? embed = null, RequestOptions? options = null)
        => Interaction.FollowupWithFilesAsync(
            attachments, text, embeds, isTTS,
            ephemeral, allowedMentions, components,
            embed, options);

    /// <inheritdoc />
    public Task<IUserMessage> GetOriginalResponseAsync(RequestOptions? options = null)
        => Interaction.GetOriginalResponseAsync(options);

    /// <inheritdoc />
    public Task<IUserMessage> ModifyOriginalResponseAsync(Action<MessageProperties> func,
        RequestOptions? options = null)
        => Interaction.ModifyOriginalResponseAsync(func, options);

    /// <inheritdoc />
    public bool HasResponded => Interaction.HasResponded;

    /// <inheritdoc />
    public IDiscordInteractionData Data => Interaction.Data;

    /// <inheritdoc />
    public int Version => Interaction.Version;

    /// <inheritdoc />
    public InteractionType Type => Interaction.Type;

    /// <inheritdoc />
    public string GuildLocale => Interaction.GuildLocale;

    /// <inheritdoc />
    public string Token => Interaction.Token;

    /// <inheritdoc />
    public string UserLocale => Interaction.UserLocale;

    /// <inheritdoc />
    public IDiscordInteraction Interaction { get; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt => Interaction.CreatedAt;

    /// <inheritdoc />
    public override Task ReplyAsync(
        string? message = null, bool isTTS = false, Embed? embed = null, RequestOptions? options = null,
        AllowedMentions? allowedMentions = null, MessageReference? messageReference = null,
        MessageComponent? components = null, ISticker[]? stickers = null, Embed[]? embeds = null,
        bool ephemeral = false)
        => _deferred && !_followed
            ? FollowupAsync(
                message, embeds, isTTS, ephemeral,
                allowedMentions, components,
                embed, options)
            : RespondAsync(
                message, embeds, isTTS, ephemeral,
                allowedMentions, components,
                embed, options);
}