using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Hangfire;
using HuTao.Data;
using HuTao.Data.Models.Authorization;
using HuTao.Data.Models.TimeTracking;
using HuTao.Services.Core.Listeners;
using HuTao.Services.Core.Preconditions.Commands;
using HuTao.Services.TimeTracking;
using HuTao.Services.Utilities;

namespace HuTao.Bot.Modules.Configuration;

[Name("Time Tracking")]
[Group("time")]
[Summary("Time tracking module.")]
[RequireAuthorization(AuthorizationScope.Configuration)]
public class TimeTrackingModule(CommandErrorHandler error, GenshinTimeTrackingService time, HuTaoContext db)
    : ModuleBase<SocketCommandContext>
{
    [Command("genshin")]
    public async Task GenshinAsync(ITextChannel channel)
    {
        var setup = new ComponentBuilderV2()
            .WithContainer(new ContainerBuilder()
                .WithTextDisplay("## Genshin Time Tracking\nSetting up…")
                .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                .WithTextDisplay("-# This message will be updated automatically.")
                .WithAccentColor(0x9B59FF))
            .Build();

        var message = await channel.SendMessageAsync(components: setup, allowedMentions: AllowedMentions.None);
        await GenshinAsync(message);
    }

    [Command("genshin")]
    public async Task GenshinAsync(IUserMessage? message = null)
    {
        if (message is null)
        {
            var setup = new ComponentBuilderV2()
                .WithContainer(new ContainerBuilder()
                    .WithTextDisplay("## Genshin Time Tracking\nSetting up…")
                    .WithSeparator(isDivider: false, spacing: SeparatorSpacingSize.Small)
                    .WithTextDisplay("-# This message will be updated automatically.")
                    .WithAccentColor(0x9B59FF))
                .Build();

            message = await ReplyAsync(components: setup, allowedMentions: AllowedMentions.None);
        }

        if (message.Channel is SocketGuildChannel channel
            && channel.Guild.Id != Context.Guild.Id)
        {
            await error.AssociateError(Context.Message, "Invalid message.");
            return;
        }

        var guild = await db.Guilds.TrackGuildAsync(Context.Guild);
        guild.GenshinRules ??= new GenshinTimeTrackingRules();

        var serverStatus = guild.GenshinRules.ServerStatus;
        if (serverStatus is not null)
        {
            var removed = db.Remove(serverStatus).Entity;
            RecurringJob.RemoveIfExists(removed.Id.ToString());
        }

        guild.GenshinRules.ServerStatus = new MessageTimeTracking
        {
            GuildId   = guild.Id,
            ChannelId = message.Channel.Id,
            MessageId = message.Id
        };

        await db.SaveChangesAsync();
        await time.TrackGenshinTime(guild);

        await Context.Message.AddReactionAsync(new Emoji("✅"));
    }
}