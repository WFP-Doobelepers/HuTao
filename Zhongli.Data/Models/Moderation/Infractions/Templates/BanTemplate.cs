using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Templates;

public class BanTemplate : ModerationTemplate, IBan
{
    protected BanTemplate() { }

    public BanTemplate(uint deleteDays, TimeSpan? length, TemplateDetails details) : base(details)
    {
        DeleteDays = deleteDays;
        Length     = length;
    }

    public uint DeleteDays { get; set; }

    public TimeSpan? Length { get; set; }
}

public class BanTemplateConfiguration : IEntityTypeConfiguration<BanTemplate>
{
    public void Configure(EntityTypeBuilder<BanTemplate> builder) => builder
        .Property(t => t.Length)
        .HasColumnName(nameof(BanTemplate.Length));
}