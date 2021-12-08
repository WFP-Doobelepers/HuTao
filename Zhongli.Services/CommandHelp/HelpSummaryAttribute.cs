using System;
using Discord.Commands;

namespace Zhongli.Services.CommandHelp;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Enum | AttributeTargets.Field)]
public class HelpSummaryAttribute : SummaryAttribute
{
    public HelpSummaryAttribute(string text) : base(text) { }
}