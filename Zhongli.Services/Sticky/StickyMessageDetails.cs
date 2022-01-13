using Discord;

namespace Zhongli.Services.Sticky;

public class StickyMessageDetails
{
    public int MessageCount { get; set; }

    public IUserMessage? Message { get; set; }
}