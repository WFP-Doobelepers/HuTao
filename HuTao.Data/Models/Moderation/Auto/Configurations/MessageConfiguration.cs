using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public class MessageConfiguration : AutoConfiguration
{
    protected MessageConfiguration() { }

    public MessageConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options)
        : base(reprimand, options) { }
}