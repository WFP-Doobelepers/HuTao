using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public class EmojiConfiguration : AutoConfiguration
{
    protected EmojiConfiguration() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public EmojiConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options)
        : base(reprimand, options) { }
}