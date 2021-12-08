using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Discord;
using Image = Zhongli.Data.Models.Discord.Message.IImage;

namespace Zhongli.Data.Models.Discord.Message;

public class Attachment : IAttachment, IImage
{
    protected Attachment() { }

    public Attachment(IAttachment attachment)
    {
        Id       = attachment.Id;
        Size     = attachment.Size;
        Height   = attachment.Height;
        Width    = attachment.Width;
        Filename = attachment.Filename;
        ProxyUrl = attachment.ProxyUrl;
        Url      = attachment.Url;
    }

    [Key] public Guid AttachmentId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    public int Size { get; set; }

    public int? Height { get; set; }

    public int? Width { get; set; }

    public string Filename { get; set; }

    public string ProxyUrl { get; set; }

    public string Url { get; set; }
}