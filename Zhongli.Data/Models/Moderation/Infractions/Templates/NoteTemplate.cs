namespace Zhongli.Data.Models.Moderation.Infractions.Templates;

public class NoteTemplate : ModerationTemplate, INote
{
    protected NoteTemplate() { }

    public NoteTemplate(TemplateDetails details) : base(details) { }
}