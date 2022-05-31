using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Discord;

namespace HuTao.Data.Models.Discord;

/// <inheritdoc cref="IDiscordInteraction" />
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class InteractionContext : Context, IInteractionContext, IDiscordInteraction
{
    public InteractionContext(IInteractionContext context)
        : base(context.Client, context.Guild, context.Channel, context.User)
    {
        Interaction = context.Interaction;
    }

    ulong IDiscordInteraction.Id => Interaction.Id;

    ulong IEntity<ulong>.Id => Interaction.Id;

    public override async Task DeferAsync(bool ephemeral = false, RequestOptions? options = null)
        => await Interaction.DeferAsync(ephemeral, options);

    public Task DeleteOriginalResponseAsync(RequestOptions? options = null)
        => Interaction.DeleteOriginalResponseAsync(options);

    public Task RespondAsync(
        string? text = null, Embed[]? embeds = null, bool isTTS = false, bool ephemeral = false,
        AllowedMentions? allowedMentions = null, MessageComponent? components = null, Embed? embed = null,
        RequestOptions? options = null)
        => Interaction.RespondAsync(
            text, embeds, isTTS, ephemeral,
            allowedMentions, components, embed,
            options);

    public Task RespondWithFilesAsync(
        IEnumerable<FileAttachment> attachments, string? text = null, Embed[]? embeds = null, bool isTTS = false,
        bool ephemeral = false, AllowedMentions? allowedMentions = null, MessageComponent? components = null,
        Embed? embed = null, RequestOptions? options = null)
        => Interaction.RespondWithFilesAsync(
            attachments, text, embeds, isTTS,
            ephemeral, allowedMentions, components,
            embed, options);

    public Task RespondWithModalAsync(Modal modal, RequestOptions? options = null)
        => Interaction.RespondWithModalAsync(modal, options);

    public async Task<IUserMessage> FollowupAsync(
        string? text = null, Embed[]? embeds = null, bool isTTS = false,
        bool ephemeral = false, AllowedMentions? allowedMentions = null, MessageComponent? components = null,
        Embed? embed = null, RequestOptions? options = null)
        => await Interaction.FollowupAsync(
            text, embeds, isTTS,
            ephemeral, allowedMentions, components,
            embed, options);

    public Task<IUserMessage> FollowupWithFilesAsync(
        IEnumerable<FileAttachment> attachments, string? text = null, Embed[]? embeds = null, bool isTTS = false,
        bool ephemeral = false, AllowedMentions? allowedMentions = null, MessageComponent? components = null,
        Embed? embed = null, RequestOptions? options = null)
        => Interaction.FollowupWithFilesAsync(
            attachments, text, embeds, isTTS,
            ephemeral, allowedMentions, components,
            embed, options);

    public Task<IUserMessage> GetOriginalResponseAsync(RequestOptions? options = null)
        => Interaction.GetOriginalResponseAsync(options);

    public Task<IUserMessage> ModifyOriginalResponseAsync(Action<MessageProperties> func,
        RequestOptions? options = null)
        => Interaction.ModifyOriginalResponseAsync(func, options);

    public bool HasResponded => Interaction.HasResponded;

    public bool IsDMInteraction => Interaction.IsDMInteraction;

    public IDiscordInteractionData Data => Interaction.Data;

    public int Version => Interaction.Version;

    public InteractionType Type => Interaction.Type;

    public string GuildLocale => Interaction.GuildLocale;

    public string Token => Interaction.Token;

    public string UserLocale => Interaction.UserLocale;

    public ulong ApplicationId => Interaction.ApplicationId;

    public ulong? ChannelId => Interaction.ChannelId;

    public ulong? GuildId => Interaction.GuildId;

    public IDiscordInteraction Interaction { get; }

    public DateTimeOffset CreatedAt => Interaction.CreatedAt;

    public override Task ReplyAsync(
        string? message = null, bool isTTS = false, Embed? embed = null, RequestOptions? options = null,
        AllowedMentions? allowedMentions = null, MessageReference? messageReference = null,
        MessageComponent? components = null, ISticker[]? stickers = null, Embed[]? embeds = null,
        MessageFlags flags = MessageFlags.None, bool ephemeral = false)
        => HasResponded
            ? FollowupAsync(
                message, embeds, isTTS, ephemeral,
                allowedMentions, components,
                embed, options)
            : RespondAsync(
                message, embeds, isTTS, ephemeral,
                allowedMentions, components,
                embed, options);
}