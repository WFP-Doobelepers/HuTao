using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Kick : ReprimandActionBase
    {
        protected Kick() { }

        public Kick(ReprimandDetails details) : base(details) { }
    }
}