using System;

namespace HuTao.Services.CommandHelp;

/// <summary>
///     Indicates tags to use during help searches to increase the hit rate of the module.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class HelpTagsAttribute(params string[] tags) : Attribute
{
    public string[] Tags { get; } = tags;
}