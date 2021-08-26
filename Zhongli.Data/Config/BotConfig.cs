namespace Zhongli.Data.Config
{
    public class BotConfig
    {
        public int MessageCacheSize { get; set; }

        public string HangfireContext { get; init; }

        public string Prefix { get; init; }

        public string Token { get; init; }

        public string ZhongliContext { get; init; }

        public ulong Owner { get; init; }
    }
}