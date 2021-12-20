using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Zhongli.Services.Interactive.Criteria;
using Zhongli.Services.Interactive.TryParse;
using Zhongli.Services.Interactive.TypeReaders;

namespace Zhongli.Services.Interactive;

public partial class PromptCollection<T> where T : notnull
{
    public PromptCollection<T> WithCriterion(ICriterion<SocketMessage> criterion) =>
        this.Modify(c => c.Criteria?.Add(criterion));

    public PromptCollection<T> WithError(string error) => this.Modify(c => c.ErrorMessage = error);

    public PromptCollection<T> WithTimeout(int timeout) => this.Modify(c => c.Timeout = timeout);

    public PromptCollection<T> WithTypeReader(TypeReader reader) => this.Modify(p => p.TypeReader = reader);

    public PromptOrCollection<T> WithPrompt(
        T key, string question,
        IEnumerable<EmbedFieldBuilder>? fields = null,
        bool required = true, int? timeOut = null)
    {
        var prompt = new Prompt<T>(key, question, fields, required, timeOut);
        this.Modify(c => c.Prompts.Add(prompt));
        return new PromptOrCollection<T>(prompt, this);
    }

    public async Task<ResultDictionary<T>> GetAnswersAsync(bool deleteResponse = true)
    {
        var ret = new Dictionary<T, PromptResult>();
        IUserMessage? message = null;

        foreach (var prompt in Prompts)
        {
            var criteria =
                this.GetCriteria()
                    .Concat(prompt.GetCriteria())
                    .AsCriterion();

            var options = new PromptOptions
            {
                Criterion      = criteria,
                Fields         = prompt.Fields,
                SecondsTimeout = prompt.Timeout ?? Timeout,
                IsRequired     = prompt.IsRequired
            };
            var result = await Module.Prompt(prompt.Question, deleteResponse ? message : null, options);

            message = result.message;

            if (result.response is null)
            {
                if (!prompt.IsRequired)
                    continue;

                if (ErrorMessage is not null)
                {
                    await Module.ModifyOrSendMessage(ErrorMessage ?? "You did not respond in time.", message,
                        new PromptOptions { Color = Color.Red });
                }

                throw new ArgumentNullException(nameof(result.response), "User did not respond in time");
            }

            object response = result.response.Content;
            if (prompt.TypeReader is not null)
                response = await prompt.TypeReader.ReadAsync(Context, response.ToString(), Services);
            else if (TypeReader is not null)
                response = await TypeReader.ReadAsync(Context, response.ToString(), Services);

            var promptResult = new PromptResult(prompt.Question, response);

            ret[prompt.Key] = promptResult;
        }

        return new ResultDictionary<T>(ret);
    }
}

public partial class PromptOrCollection<TOptions>
{
    public PromptCollection<TOptions> ThatHas(TypeReader reader)
    {
        if (!Prompt.IsRequired)
            reader = new OptionalTypeReader(reader);

        Prompt.Modify(p => p.TypeReader = reader);
        return Collection;
    }

    public PromptCollection<TOptions> ThatHas<T>(TryParseDelegate<T> tryParse) =>
        ThatHas(tryParse.AsTypeReader());

    public PromptCollection<TOptions> ThatHas<T>(EnumTryParseDelegate<T> tryParse, bool ignoreCase = true)
        where T : struct, Enum =>
        ThatHas(tryParse.AsTypeReader(ignoreCase));

    public PromptCollection<TOptions> ThatHas(ICriterion<SocketMessage> criterion)
    {
        Prompt.Modify(p => p.Criteria?.Add(criterion));
        return Collection;
    }

    public PromptOrCollection<TOptions> WithPrompt(
        TOptions key, string question,
        IEnumerable<EmbedFieldBuilder>? fields = null,
        bool required = true, int? timeOut = null)
    {
        var prompt = new Prompt<TOptions>(key, question, fields, required, timeOut);
        var collection = Collection.Modify(c => c.Prompts.Add(prompt));
        return new PromptOrCollection<TOptions>(prompt, collection);
    }

    public Task<ResultDictionary<TOptions>> GetAnswersAsync(bool deleteResponse = true) =>
        Collection.GetAnswersAsync(deleteResponse);
}

public static class PromptBuilderExtensions
{
    public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        where TKey : notnull
        where TValue : notnull =>
        dict.TryGetValue(key, out var result) ? result : value;

    internal static T Modify<T>(this T t, Action<T> action)
    {
        action(t);
        return t;
    }
}