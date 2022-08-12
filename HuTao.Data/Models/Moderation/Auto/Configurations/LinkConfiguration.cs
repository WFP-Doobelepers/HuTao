using System;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Moderation.Infractions.Actions;

namespace HuTao.Data.Models.Moderation.Auto.Configurations;

public class LinkConfiguration : AutoConfiguration
{
    protected LinkConfiguration() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public LinkConfiguration(ReprimandAction? reprimand, IAutoConfigurationOptions options)
        : base(reprimand, options) { }
}

public class Link : IEquatable<Link>, IEquatable<string>, IEquatable<Uri>
{
    public Link(Uri uri) { OriginalString = uri.OriginalString; }

    protected Link() { }

    public Guid Id { get; set; }

    public string OriginalString { get; set; } = null!;

    public Uri Uri => new(OriginalString);

    public bool Equals(Link? other) => Equals(other?.Uri);

    public bool Equals(string? other) => Equals((object?) other);

    public bool Equals(Uri? other) => Equals((object?) other);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;

        return obj switch
        {
            Uri uri => uri == Uri
                || (Uri.Host == uri.Host
                    && (Uri.AbsolutePath == uri.AbsolutePath
                        || Uri.AbsolutePath == "/")),
            Link link  => Equals(link),
            string str => Equals(str),
            _          => false
        };
    }

    public override int GetHashCode() => Uri.GetHashCode();

    public static implicit operator Link?(string? uri)
        => string.IsNullOrWhiteSpace(uri) ? null : new Link(new Uri(uri));

    public static implicit operator Link?(Uri? uri)
        => uri is null ? null : new Link(uri);
}