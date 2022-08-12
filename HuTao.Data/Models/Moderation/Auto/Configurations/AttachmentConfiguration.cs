using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public class AttachmentConfiguration : AutoConfiguration
{
    protected AttachmentConfiguration() { }

    public AttachmentConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options)
        : base(reprimand, options) { }
}