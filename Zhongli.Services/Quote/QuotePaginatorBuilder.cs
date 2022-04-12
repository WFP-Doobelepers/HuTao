using System.Collections.Generic;
using Fergun.Interactive.Pagination;

namespace Zhongli.Services.Quote;

public class QuotePaginatorBuilder : PaginatorBuilder<QuotePaginator, QuotePaginatorBuilder>
{
    internal List<QuotedPage> QuotedPages { get; } = new();

    public override QuotePaginator Build() => new(this);

    public QuotePaginatorBuilder AddPage(QuotedPage page)
    {
        QuotedPages.Add(page);
        return this;
    }
}