using System;

namespace Zhongli.Data.Models.Moderation.Infractions.Censors
{
    public class KickCensor : Censor, IKick
    {
        protected KickCensor() { }

        public KickCensor(string pattern, ICensorOptions? options) : base(pattern, options) { }
    }
}