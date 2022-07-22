using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using HuTao.Services.Image.ColorQuantization;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp.PixelFormats;
using static System.Drawing.Color;
using static SixLabors.ImageSharp.Image;

namespace HuTao.Services.Image;

/// <summary>
///     Desribes a service that performs actions related to images.
/// </summary>
public interface IImageService
{
    /// <summary>
    ///     Identifies a dominant color from the provided image.
    /// </summary>
    /// <param name="imageBytes">The bytes that compose the image for which the dominant color is to be retrieved.</param>
    /// <returns>A dominant color in the provided image.</returns>
    Color GetDominantColor(byte[] imageBytes);

    /// <summary>
    ///     Gets the dominant color of a user's avatar.
    /// </summary>
    /// <param name="contextUser">The user to retrieve the avatar from.</param>
    /// <returns>A dominant color of the user.</returns>
    ValueTask<Color> GetAvatarColor(IUser contextUser);

    /// <summary>
    ///     Identifies a dominant color from the image at the supplied location.
    /// </summary>
    /// <param name="location">The location of the image.</param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that will complete when the operation completes,
    ///     containing a dominant color in the image.
    /// </returns>
    ValueTask<Color> GetDominantColorAsync(Uri location);
}

public sealed class ImageService : IImageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public ImageService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache             = cache;
    }

    /// <inheritdoc />
    public Color GetDominantColor(byte[] imageBytes)
    {
        var colorTree = new Octree();

        using var img = Load<Rgba32>(imageBytes);
        for (var x = 0; x < img.Width; x++)
        {
            for (var y = 0; y < img.Height; y++)
            {
                var pixel = img[y, x];

                // Don't include transparent pixels
                if (pixel.A <= 127) continue;

                colorTree.Add(FromArgb(pixel.A, pixel.R, pixel.G, pixel.B));
            }
        }

        for (var i = 0; i < 7; i++)
        {
            colorTree.Reduce();
        }

        var dominant = colorTree.GetPalette()
            .DefaultIfEmpty(default)
            .MaxBy(x => x.Weight * x.Color.GetSaturation());

        return (Color) dominant.Color;
    }

    /// <inheritdoc />
    public ValueTask<Color> GetAvatarColor(IUser contextUser)
    {
        ValueTask<Color> colorTask = default;

        if ((contextUser.GetAvatarUrl(size: 16) ?? contextUser.GetDefaultAvatarUrl()) is { } avatarUrl)
            colorTask = GetDominantColorAsync(new Uri(avatarUrl));

        return colorTask;
    }

    /// <inheritdoc />
    public async ValueTask<Color> GetDominantColorAsync(Uri location)
    {
        var key = GetKey(location);

        if (_cache.TryGetValue(key, out Color color))
            return color;

        try
        {
            var imageBytes = await _httpClientFactory.CreateClient().GetByteArrayAsync(location);
            return _cache.Set(key, GetDominantColor(imageBytes), TimeSpan.FromHours(1));
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.NotFound)
        {
            return Color.Default;
        }
    }

    private static object GetKey(Uri uri) => new { Target = nameof(GetDominantColor), uri.AbsoluteUri };
}