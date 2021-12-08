using System;
using System.Net.Http;
using System.Threading.Tasks;
using ColorThiefDotNet;
using Discord;
using Microsoft.Extensions.Caching.Memory;
using Zhongli.Services.Utilities;
using Color = Discord.Color;

namespace Zhongli.Services.Image;

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
    private readonly ColorThief _colorThief = new();
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
        var quantizedColor = _colorThief.GetColor(imageBytes.ToBitmap(), 8);

        return quantizedColor.ToDiscordColor();
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

        if (_cache.TryGetValue(key, out Color color)) return color;

        var imageBytes = await _httpClientFactory.CreateClient().GetByteArrayAsync(location);
        color = GetDominantColor(imageBytes);

        _cache.Set(key, color, TimeSpan.FromDays(7));

        return color;
    }

    private static object GetKey(Uri uri) => new { Target = "DominantColor", uri.AbsoluteUri };
}