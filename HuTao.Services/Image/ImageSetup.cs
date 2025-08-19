using Microsoft.Extensions.DependencyInjection;

namespace HuTao.Services.Image;

public static class ImageSetup
{
    public static IServiceCollection AddImages(this IServiceCollection services) =>
        services.AddScoped<IImageService, ImageService>()
                .AddScoped<IReprimandHistoryImageService, ReprimandHistoryImageService>();
}