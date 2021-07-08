using System;
using System.Text.RegularExpressions;
using Discord;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public abstract class Censor : IModerationAction
    {
        protected Censor() { }

        protected Censor(string pattern, RegexOptions options = RegexOptions.None)
        {
            Pattern = pattern;
            Options = options;
        }

        public Guid Id { get; set; }

        public string Pattern { get; set; }

        public RegexOptions Options { get; set; }

        public virtual ModerationAction Action { get; set; }

        public bool IsMatch(IMessage message) => Regex.IsMatch(message.Content, Pattern,
            Options |= RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        public Match Match(IMessage message) => Regex.Match(message.Content, Pattern,
            Options |= RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        public MatchCollection Matches(IMessage message) => Regex.Matches(message.Content, Pattern,
            Options |= RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    }
}