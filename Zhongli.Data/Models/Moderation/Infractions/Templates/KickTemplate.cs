namespace Zhongli.Data.Models.Moderation.Infractions.Templates;

public class KickTemplate : ModerationTemplate, IKick
{
    protected KickTemplate() { }

    public KickTemplate(TemplateDetails details) : base(details) { }
}