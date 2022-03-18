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
        Id          = attachment.Id;
        Ephemeral   = attachment.Ephemeral;
        Size        = attachment.Size;
        Height      = attachment.Height;
        Width       = attachment.Width;
        Filename    = attachment.Filename;
        ProxyUrl    = attachment.ProxyUrl;
        Url         = attachment.Url;
        ContentType = attachment.ContentType;
        Description = attachment.Description;
    }

    [Key] public Guid AttachmentId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; init; }

    public bool Ephemeral { get; init; }

    public int Size { get; init; }

    public int? Height { get; init; }

    public int? Width { get; init; }

    public string Filename { get; init; } = null!;

    public string ProxyUrl { get; init; } = null!;

    public string Url { get; init; } = null!;

    public string? ContentType { get; set; }

    public string? Description { get; set; }
}