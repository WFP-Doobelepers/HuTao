using System;
using System.Diagnostics.CodeAnalysis;
using HuTao.Data.Models.Criteria;
using HuTao.Data.Models.Moderation.Auto.Configurations;

namespace HuTao.Data.Models.Moderation.Auto.Exclusions;

public class LinkExclusion : ModerationExclusion, IJudge<Uri>
{
    protected LinkExclusion() { }

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public LinkExclusion(Link link, AutoConfiguration? config) : base(config) { Link = link; }

    public virtual Link Link { get; set; } = null!;

    public bool Judge(Uri uri) => Link.Equals(uri);
}