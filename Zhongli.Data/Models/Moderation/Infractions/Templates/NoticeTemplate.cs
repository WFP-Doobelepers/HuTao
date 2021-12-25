namespace Zhongli.Data.Models.Moderation.Infractions.Templates;

public class NoticeTemplate : ModerationTemplate, INotice
{
    protected NoticeTemplate() { }

    public NoticeTemplate(TemplateDetails details) : base(details) { }
}