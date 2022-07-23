using System;
using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public class DuplicateConfiguration : AutoConfiguration
{
    public enum DuplicateType
    {
        Message,
        Word,
        Character
    }

    protected DuplicateConfiguration() { }

    public DuplicateConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options)
        : base(reprimand, options)
    {
        Type       = options.DuplicateType;
        Percentage = Math.Min(options.DuplicatePercentage / 100, 1);
        Tolerance  = options.DuplicateTolerance;
    }

    public double Percentage { get; set; }

    public DuplicateType Type { get; set; }

    public int Tolerance { get; set; }
}