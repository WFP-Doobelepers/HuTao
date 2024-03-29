using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;

namespace HuTao.Services.Quote;

public class QuotedPage : IPage
{
    internal QuotedPage(QuotedMessage quote, MultiEmbedPageBuilder builder)
    {
        Quote            = quote;
        Text             = builder.Text;
        IsTTS            = builder.IsTTS;
        AllowedMentions  = builder.AllowedMentions;
        MessageReference = builder.MessageReference;
        Stickers         = builder.Stickers;
        EmbedArray       = builder.Builders;
    }

    public IEnumerable<EmbedBuilder> EmbedArray { get; set; }

    public QuotedMessage Quote { get; }

    Embed[] IPage.GetEmbedArray() => EmbedArray.Select(e => e.Build()).ToArray();

    public AllowedMentions? AllowedMentions { get; }

    public bool IsTTS { get; }

    public Func<ValueTask<IEnumerable<FileAttachment>?>>? AttachmentsFactory { get; set; }

    public IReadOnlyCollection<Embed> Embeds => EmbedArray.Select(e => e.Build()).ToArray();

    public IReadOnlyCollection<ISticker> Stickers { get; }

    public MessageReference? MessageReference { get; }

    public string? Text { get; }
}