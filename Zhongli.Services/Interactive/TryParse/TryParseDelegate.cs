namespace Zhongli.Services.Interactive.TryParse
{
    public delegate bool TryParseDelegate<T>(string input, out T result);

    public delegate bool EnumTryParseDelegate<T>(string input, bool ignoreCase, out T result);
}