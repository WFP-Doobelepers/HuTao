using System;
using Discord;

namespace Zhongli.Data.Models.Discord.Message;

public class Author
{
    protected Author() { }

    public Author(EmbedAuthor author)
    {
        Name         = author.Name;
        Url          = author.Url;
        IconUrl      = author.IconUrl;
        ProxyIconUrl = author.ProxyIconUrl;
    }

    public Guid Id { get; set; }

    /// <inheritdoc cref="EmbedAuthor.IconUrl" />
    public string IconUrl { get; set; } = null!;

    /// <inheritdoc cref="EmbedAuthor.Name" />
    public string Name { get; set; } = null!;

    /// <inheritdoc cref="EmbedAuthor.ProxyIconUrl" />
    public string ProxyIconUrl { get; set; } = null!;

    /// <inheritdoc cref="EmbedAuthor.Url" />
    public string Url { get; set; } = null!;

    public static implicit operator Author(EmbedAuthor author) => new(author);
}