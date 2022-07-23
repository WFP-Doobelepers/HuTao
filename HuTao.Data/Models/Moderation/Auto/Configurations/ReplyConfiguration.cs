using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public class ReplyConfiguration : AutoConfiguration
{
    protected ReplyConfiguration() { }

    public ReplyConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options)
        : base(reprimand, options) { }
}