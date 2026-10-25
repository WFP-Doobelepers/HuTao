using System.Linq;
using Discord.Interactions;
using HuTao.Services.Core;
using Xunit;
using IModuleBase = Discord.Commands.IModuleBase;

namespace HuTao.Tests.Bot;

// The bot adds text and interaction modules only from its own assembly, so a module in HuTao.Services is never
// registered. Such a copy keeps the checks it had when it was copied, and registering that assembly later would bring
// them back, so HuTao.Services holds only the abstract bases that the bot's modules derive from.
public class ModuleLocationTests
{
    [Fact]
    public void NoModuleLivesOutsideTheBot()
    {
        var modules = typeof(AuthorizationService).Assembly.GetTypes()
            .Where(t => !t.IsAbstract
                && (typeof(IModuleBase).IsAssignableFrom(t) || typeof(IInteractionModuleBase).IsAssignableFrom(t)))
            .Select(t => t.FullName)
            .Order()
            .ToList();

        Assert.Empty(modules);
    }
}
