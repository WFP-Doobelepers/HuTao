using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.Discord.Message.Linking;
using HuTao.Services.CommandHelp;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.Interactive;
using HuTao.Services.Linking;
using HuTao.Services.Sticky;

namespace HuTao.Bot.Modules.Linking;

[Name("Sticky Messages")]
[Group("sticky")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class StickyModule : InteractiveEntity<StickyMessage>
{
    private readonly CommandErrorHandler _error;
    private readonly StickyService _sticky;

    public StickyModule(CommandErrorHandler error, StickyService sticky)
    {
        _error  = error;
        _sticky = sticky;
    }

    [Command("add")]
    [RequireContext(ContextType.Guild)]
    public async Task AddStickyMessage(
        [Summary("The message that the sticky message will have. This will copy the embed structure as is.")]
        IMessage message,
        [Summary("The various options your sticky message will have.")]
        StickyMessageOptions? options = null)
    {
        var channel = options?.Channel ?? (ITextChannel) Context.Channel;
        var template = new MessageTemplate(message, options);
        var sticky = new StickyMessage(template, channel, options);

        await _sticky.AddAsync(sticky, (IGuildChannel) Context.Channel);
        await _sticky.SendStickyMessage(sticky, channel);
    }

    [Command("disable")]
    [RequireContext(ContextType.Guild)]
    public async Task DisableStickyMessageAsync(
        [Summary("The ID of the sticky message to disable.")] string id)
    {
        var sticky = await TryFindEntityAsync(id, await GetCollectionAsync());
        if (sticky == null)
            await _error.AssociateError(Context.Message, "Could not find sticky message with that ID.");
        else
        {
            await _sticky.DisableAsync(sticky);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }

    [Command("enable")]
    public async Task EnableStickyMessageAsync(
        [Summary("The ID of the sticky message to enable.")] string id)
    {
        var sticky = await TryFindEntityAsync(id, await GetCollectionAsync());
        if (sticky == null)
            await _error.AssociateError(Context.Message, "Could not find sticky message with that ID.");
        else
        {
            await _sticky.EnableAsync(sticky, (IGuildChannel) Context.Channel);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }
    }

    [Command("remove")]
    [Alias("delete")]
    [Summary("Remove a sticky message.")]
    protected override Task RemoveEntityAsync(string id) => base.RemoveEntityAsync(id);

    [Command("view")]
    [Alias("list")]
    [Summary("View sticky messages.")]
    protected override Task ViewEntityAsync() => base.ViewEntityAsync();

    protected override EmbedBuilder EntityViewer(StickyMessage entity)
    {
        var template = entity.Template;
        return new EmbedBuilder()
            .AddField("Template ID", template.Id, true)
            .AddField("Channel", $"<#{entity.ChannelId}>", true)
            .WithTemplateDetails(template, Context.Guild)
            .AddField("Active", entity.IsActive, true)
            .AddField("Time Delay", entity.TimeDelay?.Humanize() ?? "None", true)
            .AddField("Count Delay", entity.CountDelay ?? 0, true)
            .WithTitle($"Sticky: {entity.Id}");
    }

    protected override string Id(StickyMessage entity) => entity.Id.ToString();

    protected override Task RemoveEntityAsync(StickyMessage entity) => _sticky.DeleteAsync(entity);

    protected override Task<ICollection<StickyMessage>> GetCollectionAsync()
        => _sticky.GetStickyMessages(Context.Guild);

    [NamedArgumentType]
    public class StickyMessageOptions : IMessageTemplateOptions, IStickyMessageOptions
    {
        [HelpSummary("`True` to allow mentions and `False` to not.")]
        public bool AllowMentions { get; set; }

        [HelpSummary("`True` if you want the message to be live, where it will update its contents continuously.")]
        public bool IsLive { get; set; }

        [HelpSummary("`True` if you want embed timestamps to use the current time, `False` if not.")]
        public bool ReplaceTimestamps { get; set; }

        [HelpSummary("`True` if you want embeds to be suppressed, `False` if not.")]
        public bool SuppressEmbeds { get; set; }

        [HelpSummary("`False` if you don't want this sticky to replace the current active one. Defaults to `True`.")]
        public bool IsActive { get; set; } = true;

        [HelpSummary("Optionally the text channel that the sticky message will be sent to.")]
        public ITextChannel? Channel { get; set; }

        [HelpSummary("The time delay to wait before sending another sticky.")]
        public TimeSpan? TimeDelay { get; set; }

        [HelpSummary("The message count delay to wait before sending another sticky.")]
        public uint? CountDelay { get; set; }
    }
}