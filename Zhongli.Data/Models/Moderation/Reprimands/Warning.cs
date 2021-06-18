using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Zhongli.Data.Models.Moderation.Reprimands
{
    public class Warning : ReprimandActionBase
    {
        protected Warning() { }

        public Warning(ReprimandDetails details, uint amount) : base(details) { Amount = amount; }

        public uint Amount { get; init; }
    }
}