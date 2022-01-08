using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Footer
{
    protected Footer() { }

    public Footer(EmbedFooter footer)
    {
        IconUrl  = footer.IconUrl;
        ProxyUrl = footer.ProxyUrl;
        Text     = footer.Text;
    }

    public Guid Id { get; set; }

    /// <inheritdoc cref="EmbedFooter.IconUrl" />
    public string IconUrl { get; set; }

    /// <inheritdoc cref="EmbedFooter.ProxyUrl" />
    public string ProxyUrl { get; set; }

    /// <inheritdoc cref="EmbedFooter.Text" />
    public string Text { get; set; }

    public static implicit operator Footer(EmbedFooter footer) => new(footer);
}