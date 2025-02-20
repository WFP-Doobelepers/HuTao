using System;
using Discord;

namespace HuTao.Data.Models.Discord.Message.Embeds;

public class Footer : IEquatable<Footer>
{
    protected Footer() { }

    public Footer(EmbedFooter footer)
    {
        IconUrl  = footer.IconUrl;
        ProxyUrl = footer.ProxyUrl;
        Text     = footer.Text;
    }

    public Guid Id { get; init; }

    /// <inheritdoc cref="EmbedFooter.Text" />
    public string Text { get; init; } = null!;

    /// <inheritdoc cref="EmbedFooter.IconUrl" />
    public string? IconUrl { get; init; }

    /// <inheritdoc cref="EmbedFooter.ProxyUrl" />
    public string? ProxyUrl { get; init; }

    /// <inheritdoc />
    public bool Equals(Footer? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Text == other.Text && IconUrl == other.IconUrl && ProxyUrl == other.ProxyUrl;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Footer other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Text, IconUrl, ProxyUrl);

    public static bool operator ==(Footer? left, Footer? right) => Equals(left, right);

    public static bool operator !=(Footer? left, Footer? right) => !Equals(left, right);

    public static implicit operator Footer(EmbedFooter footer) => new(footer);
}