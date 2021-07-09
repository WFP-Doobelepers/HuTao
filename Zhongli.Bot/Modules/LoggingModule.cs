using Discord.Commands;
using System.Threading.Tasks;
using Zhongli.Data;

namespace Zhongli.Bot.Modules
{
    [Group("Logging")]
    public class LoggingModule : ModuleBase
    {
        private readonly ZhongliContext _db;
        public LoggingModule(ZhongliContext db) { _db = db; }

        public async Task LogAction()
        {

        }
        
    }
}
