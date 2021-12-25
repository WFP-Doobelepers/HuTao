using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Infractions.Templates;

public class MuteTemplate : ModerationTemplate, IMute
{
    protected MuteTemplate() { }

    public MuteTemplate(TimeSpan? length, TemplateDetails details) : base(details) { Length = length; }

    public TimeSpan? Length { get; set; }
}

public class MuteTemplateConfiguration : IEntityTypeConfiguration<MuteTemplate>
{
    public void Configure(EntityTypeBuilder<MuteTemplate> builder)
    {
        builder
            .Property(t => t.Length)
            .HasColumnName(nameof(MuteTemplate.Length));
    }
}