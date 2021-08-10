namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class WarningCensor : Censor, IWarning
    {
        protected WarningCensor() { }

        public WarningCensor(string pattern, ICensorOptions? options, uint count) : base(pattern, options) { Count = count; }

        public uint Count { get; set; }
    }
}