using System;
using System.Drawing;
using System.Threading.Tasks;
using Discord.Commands;
using Color = Discord.Color;

namespace Zhongli.Services.Core.TypeReaders;

public class HexColorTypeReader : TypeReader
{
    public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
        IServiceProvider services)
    {
        try
        {
            var color = ColorTranslator.FromHtml($"#{input.Replace("#", string.Empty)}");
            return Task.FromResult(TypeReaderResult.FromSuccess((Color) color));
        }
        catch (Exception e)
        {
            return Task.FromResult(TypeReaderResult.FromError(e));
        }
    }
}