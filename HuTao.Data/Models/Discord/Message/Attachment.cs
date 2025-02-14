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
        Filename         = attachment.Filename;
        Size             = attachment.Size;
        ContentType      = attachment.ContentType;
        Description      = attachment.Description;
        Title            = attachment.Title;
        Url              = attachment.Url;
        ProxyUrl         = attachment.ProxyUrl;
        Height           = attachment.Height;
        Width            = attachment.Width;
        CreatedAt        = attachment.CreatedAt;
        Ephemeral        = attachment.Ephemeral;
        Flags            = attachment.Flags;
        Duration         = attachment.Duration;
        Waveform         = attachment.Waveform;
        WaveformBytes    = attachment.WaveformBytes;
        ClipCreatedAt    = attachment.ClipCreatedAt;
        ClipParticipants = attachment.ClipParticipants;
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

    [NotMapped] public double? Duration { get; set; }

    [NotMapped] public string? Waveform { get; set; }

    [NotMapped] public byte[]? WaveformBytes { get; set; }

    public AttachmentFlags Flags { get; set; }

    [NotMapped] public IReadOnlyCollection<IUser>? ClipParticipants { get; set; }

    [NotMapped] public string? Title { get; }

    [NotMapped] public DateTimeOffset? ClipCreatedAt { get; }

    public string? Description { get; set; }

    [NotMapped] public DateTimeOffset CreatedAt { get; }
}