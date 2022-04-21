namespace HuTao.Data.Models.Discord.Message;

public interface IImage
{
    /// <summary>Gets the height of this image.</summary>
    /// <returns>
    ///     A <see cref="T:System.Int32" /> representing the height of this image if it can be retrieved; otherwise
    ///     <c>null</c>.
    /// </returns>
    int? Height { get; init; }

    /// <summary>Gets the width of this image.</summary>
    /// <returns>
    ///     A <see cref="T:System.Int32" /> representing the width of this image if it can be retrieved; otherwise
    ///     <c>null</c>.
    /// </returns>
    int? Width { get; init; }

    /// <summary>Gets a proxied URL of this image.</summary>
    /// <returns>
    ///     A string containing the proxied URL of this image.
    /// </returns>
    string ProxyUrl { get; init; }

    /// <summary>Gets the URL of the image.</summary>
    /// <returns>A string containing the URL of the image.</returns>
    string Url { get; init; }
}