using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive.Criteria;
using Discord.Commands;
using Discord.WebSocket;

namespace Zhongli.Services.Interactive.TypeReaders
{
    public class TypeReaderCriterion : ICriterion<SocketMessage>
    {
        private readonly IServiceProvider? _services;
        private readonly TypeReader _reader;

        public TypeReaderCriterion(TypeReader reader, IServiceProvider? services = null)
        {
            _reader   = reader;
            _services = services;
        }

        public async Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketMessage parameter)
        {
            var result = await _reader.ReadAsync(sourceContext, parameter.Content, _services);

            return result.IsSuccess;
        }
    }
}