using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Fergun.Interactive;
using HuTao.Services.Image;
using HuTao.Services.Interactive.Criteria;
using HuTao.Services.Interactive.TypeReaders;
using HuTao.Services.Utilities;
using Optional = HuTao.Services.Interactive.TypeReaders.Optional;

namespace HuTao.Services.Interactive;

public abstract class InteractivePromptBase : ModuleBase<SocketCommandContext>
{
    public IImageService ImageService { get; init; } = null!;

    public InteractiveService Service { get; init; } = null!;

    public PromptCollection<T> CreatePromptCollection<T>(string? errorMessage = null)
        where T : notnull => new(this, errorMessage);

    internal async Task<(SocketMessage? response, IUserMessage message)> Prompt(
        string question,
        IUserMessage? message, PromptOptions? promptOptions)
    {
        message = await ModifyOrSendMessage(question, message, promptOptions);

        InteractiveResult<SocketMessage?> response;
        var timeout = TimeSpan.FromSeconds(promptOptions?.SecondsTimeout ?? 30);
        if (promptOptions?.Criterion is null)
            response = await Service.NextMessageAsync(timeout: timeout);
        else
        {
            response = await Service.NextMessageAsync(timeout: timeout,
                filter: promptOptions.Criterion.AsFunc(Context));
        }

        _ = response.Value?.DeleteAsync();

        if (!(promptOptions?.IsRequired ?? false) && (response.Value?.IsSkipped() ?? false))
            return (null, message);

        return (response.Value, message);
    }

    internal async Task<IUserMessage> ModifyOrSendMessage(
        string content,
        IUserMessage? message, PromptOptions? promptOptions)
    {
        var accent = (promptOptions?.Color ??
            await ImageService.GetDominantColorAsync(new Uri(Context.User.GetDefiniteAvatarUrl()))).RawValue;

        var container = new ContainerBuilder()
            .WithAccentColor(accent);

        var avatarUrl = Context.User.GetDisplayAvatarUrl(size: 128) ?? Context.User.GetDefiniteAvatarUrl();
        var header = new SectionBuilder()
            .WithTextDisplay($"## Prompt\n{content}")
            .WithAccessory(new ThumbnailBuilder(new UnfurledMediaItemProperties(avatarUrl)));

        container.WithSection(header);

        var fields = (promptOptions?.Fields ?? []).ToList();
        if (fields.Count > 0)
        {
            var fieldText = string.Join("\n\n", fields.Select(f => $"**{f.Name}**\n{f.Value}"));
            container
                .WithSeparator(isDivider: true, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay(fieldText);
        }

        if (!promptOptions?.IsRequired ?? false)
        {
            container
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay($"-# Reply '{Optional.SkipString}' if you don't need this.");
        }

        var components = new ComponentBuilderV2()
            .WithContainer(container)
            .Build();

        if (message is null)
        {
            return await ReplyAsync(components: components, allowedMentions: AllowedMentions.None);
        }

        await message.ModifyAsync(msg =>
        {
            msg.Components = components;
            msg.Embeds = Array.Empty<Embed>();
        });

        return message;
    }
}