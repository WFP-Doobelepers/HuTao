using System;
using Discord.Commands;

namespace HuTao.Services.CommandHelp;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Enum | AttributeTargets.Field)]
public class HelpSummaryAttribute(string text) : SummaryAttribute(text);