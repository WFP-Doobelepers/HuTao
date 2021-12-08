using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Addons.Interactive.Paginator;
using Discord.Commands;

namespace Zhongli.Services.Interactive.Paginator;

public class PaginatedDMCallback : PaginatedMessageCallback
{
    private readonly InteractiveService _interactive;
    private readonly PaginatedAppearanceOptions _options;
    private readonly PaginatedMessage _pager;

    public PaginatedDMCallback(
        InteractiveService interactive, SocketCommandContext sourceContext, PaginatedMessage pager)
        : base(interactive, sourceContext, pager)
    {
        _interactive = interactive;
        _pager       = pager;
        _options     = _pager.Options;
    }

    public new IUserMessage Message { get; private set; } = null!;

    public new async Task DisplayAsync()
    {
        var embed = BuildEmbed();
        var dm = await Context.User.GetOrCreateDMChannelAsync();
        Message = await dm.SendMessageAsync(_pager.Content, embed: embed).ConfigureAwait(false);

        _interactive.AddReactionCallback(Message, this);
        // Reactions take a while to add, don't wait for them
        _ = Task.Run(async () =>
        {
            await Message.AddReactionAsync(_options.First);
            await Message.AddReactionAsync(_options.Back);
            await Message.AddReactionAsync(_options.Next);
            await Message.AddReactionAsync(_options.Last);

            if (_options.JumpDisplayOptions == JumpDisplayOptions.Always)
                await Message.AddReactionAsync(_options.Jump);

            await Message.AddReactionAsync(_options.Stop);

            if (_options.DisplayInformationIcon)
                await Message.AddReactionAsync(_options.Info);
        });

        if (Timeout is not null)
        {
            _ = Task.Delay(Timeout.Value).ContinueWith(_ =>
            {
                _interactive.RemoveReactionCallback(Message);
                Message.DeleteAsync();
            });
        }
    }
}