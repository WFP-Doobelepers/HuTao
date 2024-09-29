using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Discord;

namespace HuTao.Data.Models.Discord.Message;

public class Attachment : IAttachment, IImage
{
    protected Attachment() { }

    public Attachment(IAttachment attachment)
    {
        Id               = attachment.Id;
        Ephemeral        = attachment.Ephemeral;
        Size             = attachment.Size;
        Height           = attachment.Height;
        Width            = attachment.Width;
        Filename         = attachment.Filename;
        ProxyUrl         = attachment.ProxyUrl;
        Url              = attachment.Url;
        ContentType      = attachment.ContentType;
        Description      = attachment.Description;
        Duration         = attachment.Duration;
        Waveform         = attachment.Waveform;
        Flags            = attachment.Flags;
        Title            = attachment.Title;
        ClipCreatedAt    = attachment.ClipCreatedAt;
        CreatedAt        = attachment.CreatedAt;
        ClipParticipants = attachment.ClipParticipants;
    }

    public AttachmentFlags Flags { get; set; }

    public bool Ephemeral { get; init; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ClipCreatedAt { get; set; }

    public double? Duration { get; set; }

    [Key] public Guid AttachmentId { get; set; }

    public int Size { get; init; }

    public int? Height { get; init; }

    public int? Width { get; init; }

    [NotMapped]
    public IReadOnlyCollection<IUser>? ClipParticipants { get; set; }

    public string Filename { get; init; } = null!;

    public string ProxyUrl { get; init; } = null!;

    public string Title { get; set; }

    public string Url { get; init; } = null!;

    public string Waveform { get; set; }

    public string? ContentType { get; set; }

    public string? Description { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; init; }
}