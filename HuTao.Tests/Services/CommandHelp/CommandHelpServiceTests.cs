using System;
using System.Collections.Generic;
using System.Reflection;
using Discord;
using HuTao.Services.CommandHelp;
using HuTao.Tests.Testing;
using Xunit;

namespace HuTao.Tests.Services.CommandHelp;

public class CommandHelpServiceTests
{
    [Fact]
    public void BuildModuleContainer_ProducesValidComponentsV2()
    {
        var commands = new List<CommandHelpData>
        {
            new()
            {
                Name = "ping",
                Summary = "Ping the bot.",
                Aliases = new[] { "ping" },
                Parameters = Array.Empty<ParameterHelpData>()
            },
            new()
            {
                Name = "echo",
                Summary = "Echo a message.",
                Aliases = new[] { "echo", "say" },
                Parameters = Array.Empty<ParameterHelpData>()
            }
        };

        var module = new ModuleHelpData
        {
            Name = "Test",
            Summary = "Test summary.",
            Commands = commands,
            HelpTags = Array.Empty<string>()
        };

        var serviceAssembly = typeof(ICommandHelpService).Assembly;
        var type = serviceAssembly.GetType("HuTao.Services.CommandHelp.CommandHelpService");
        Assert.NotNull(type);

        var method = type!.GetMethod("BuildModuleContainer", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var container = (ContainerBuilder)method!.Invoke(null, new object[] { module, commands, "h!" })!;
        var built = new ComponentBuilderV2().WithContainer(container).Build();

        Assert.NotNull(built);
        built.ShouldBeValidComponentsV2();
    }

    [Fact]
    public void BuildModuleContainer_CommandWithoutAliases_DoesNotRenderEmptyMetaLine()
    {
        var commands = new List<CommandHelpData>
        {
            new()
            {
                Name = "remove",
                Summary = "Removes specified roles to a user.",
                Aliases = new[] { "remove" },
                Parameters = Array.Empty<ParameterHelpData>()
            }
        };

        var module = new ModuleHelpData
        {
            Name = "Role",
            Summary = "Role commands.",
            Commands = commands,
            HelpTags = Array.Empty<string>()
        };

        var serviceAssembly = typeof(ICommandHelpService).Assembly;
        var type = serviceAssembly.GetType("HuTao.Services.CommandHelp.CommandHelpService");
        Assert.NotNull(type);

        var method = type!.GetMethod("BuildModuleContainer", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var container = (ContainerBuilder)method!.Invoke(null, new object[] { module, commands, "w!" })!;
        var built = new ComponentBuilderV2().WithContainer(container).Build();

        built.ShouldBeValidComponentsV2();
        var text = ExtractAllText(built);

        Assert.DoesNotContain("\n-#", text);
    }

    private static string ExtractAllText(MessageComponent components)
    {
        var sb = new System.Text.StringBuilder();
        AppendComponents(components.Components);
        return sb.ToString();

        void AppendComponents(IReadOnlyCollection<IMessageComponent> list)
        {
            foreach (var c in list)
            {
                switch (c)
                {
                    case ContainerComponent container:
                        AppendComponents(container.Components);
                        break;
                    case SectionComponent section:
                        AppendComponents(section.Components);
                        break;
                    case TextDisplayComponent textDisplay:
                        sb.AppendLine(textDisplay.Content);
                        break;
                }
            }
        }
    }
}

