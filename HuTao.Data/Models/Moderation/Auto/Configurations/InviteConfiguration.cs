using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public class InviteConfiguration : AutoConfiguration
{
    protected InviteConfiguration() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public InviteConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options)
        : base(reprimand, options) { }
}