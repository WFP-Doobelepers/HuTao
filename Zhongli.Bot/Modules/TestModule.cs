using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Zhongli.Data;

namespace Zhongli.Bot.Modules
{
    public class TestModule : ModuleBase
    {
        private readonly ZhongliContext _db;

        public TestModule(ZhongliContext db) { _db = db; }

        [Command("entity")]
        public async Task TryCreateUserEntity(IGuildUser? user = null)
        {
            user ??= (IGuildUser) Context.User;

            var userEntity = await _db.Users.FindAsync(user.Id, user.GuildId);

            _db.Remove(userEntity);
            await _db.SaveChangesAsync();
        }

        [Command("collection")]
        public async Task TryCreateUserEntity([Remainder] IEnumerable<IEmote> collection) { await ReplyAsync(string.Join(", ", collection.Select(c => c.Name))); }
    }
}