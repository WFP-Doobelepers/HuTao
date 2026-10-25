using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HuTao.Services.Image;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using Color = Discord.Color;

namespace HuTao.Tests.Services.Image;

// A failed avatar download gives the default color instead of throwing into the command.
public class ImageServiceTests
{
    private sealed class FailingFactory(Func<HttpResponseMessage> respond) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FailingHandler(respond));

        private sealed class FailingHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
                => Task.FromResult(respond());
        }
    }

    // The color is only an accent, so a CDN that fails or times out leaves the default color instead of failing the
    // command that asked for it.
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    public async Task FailedDownload_GivesDefault(HttpStatusCode status)
    {
        var service = new ImageService(new FailingFactory(() => new HttpResponseMessage(status)),
            new MemoryCache(new MemoryCacheOptions()));

        Assert.Equal(Color.Default, await service.GetDominantColorAsync(new Uri("https://cdn.test/a.png")));
    }

    [Fact]
    public async Task TimedOutDownload_GivesDefault()
    {
        var service = new ImageService(new FailingFactory(() => throw new TaskCanceledException("The request timed out.")),
            new MemoryCache(new MemoryCacheOptions()));

        Assert.Equal(Color.Default, await service.GetDominantColorAsync(new Uri("https://cdn.test/a.png")));
    }
}
