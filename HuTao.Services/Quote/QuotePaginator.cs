using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using HuTao.Services.Utilities;

namespace HuTao.Services.Quote;

public class QuotePaginator : Paginator
{
    private readonly IReadOnlyCollection<IPage> _pages;

    public QuotePaginator(QuotePaginatorBuilder builder) : base(builder) { _pages = builder.QuotedPages; }

    public override int MaxPageIndex => _pages.Count - 1;

    public override ComponentBuilder GetOrAddComponents(bool disableAll, ComponentBuilder? builder = null) => base
        .GetOrAddComponents(disableAll, builder)
        .WithQuotedMessage((_pages.ElementAtOrDefault(CurrentPageIndex) as QuotedPage)?.Quote);

    public override async Task<IPage> GetOrLoadPageAsync(int pageIndex)
    {
        var element = _pages.ElementAt(pageIndex);
        if (element is not QuotedPage page) return element;

        var message = await page.Quote.GetMessageAsync();
        if (message is null && page.EmbedArray.Any())
            page.EmbedArray = page.EmbedArray.Select(e => e.WithColor(Color.Red));

        return page;
    }
}