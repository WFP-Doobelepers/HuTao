namespace Zhongli.Data.Models.Moderation.Infractions.Templates;

public class WarningTemplate : ModerationTemplate, IWarning
{
    protected WarningTemplate() { }

    public WarningTemplate(uint count, TemplateDetails details) : base(details) { Count = count; }

    public uint Count { get; set; }
}