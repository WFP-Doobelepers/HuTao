using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Ban : ReprimandActionBase
    {
        protected Ban() { }

        public Ban(ReprimandDetails details, uint deleteDays) : base(details) { DeleteDays = deleteDays; }

        public uint DeleteDays { get; set; }
    }
}