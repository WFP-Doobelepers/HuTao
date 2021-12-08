namespace Zhongli.Data.Models.Discord.Message;

public interface IImage
{
    int? Height { get; set; }

    int? Width { get; set; }

    string ProxyUrl { get; set; }

    string Url { get; set; }
}