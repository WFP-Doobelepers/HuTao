using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public class NewLineConfiguration : AutoConfiguration
{
    protected NewLineConfiguration() { }

    public NewLineConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options)
        : base(reprimand, options)
    {
        BlankOnly = options.NewLineBlankOnly;
    }

    public bool BlankOnly { get; set; }
}