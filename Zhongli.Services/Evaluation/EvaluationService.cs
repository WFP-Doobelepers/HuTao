using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Zhongli.Data.Models.Discord;
using Zhongli.Services.Utilities;
using static Discord.EmbedFieldBuilder;

namespace Zhongli.Services.Evaluation;

public class EvaluationService
{
    private readonly IServiceProvider _services;

    public EvaluationService(IServiceProvider services) { _services = services; }

    public static EmbedBuilder BuildEmbed(Context context, EvaluationResult result)
    {
        var returnValue = JsonSerializer.Serialize(result.ReturnValue, EvaluationResult.SerializerOptions);
        var consoleOut = result.ConsoleOut;
        var status = string.IsNullOrEmpty(result.Exception) ? "Success" : "Failure";

        var compile = result.CompileTime.TotalMilliseconds;
        var execution = result.ExecutionTime.TotalMilliseconds;
        var embed = new EmbedBuilder()
            .WithUserAsAuthor(context.User)
            .WithTitle($"Eval Result: {status}")
            .WithDescription(Format.Code(result.Code, "cs"))
            .WithColor(string.IsNullOrEmpty(result.Exception) ? Color.Green : Color.Red)
            .WithFooter($"Compile: {compile:F}ms | Execution: {execution:F}ms");

        if (result.ReturnValue != null)
        {
            embed.AddField(a => a
                .WithName($"Result: {result.ReturnTypeName ?? "null"}")
                .WithValue(Format.Code(returnValue, "json").Truncate(MaxFieldValueLength)));
        }

        if (!string.IsNullOrWhiteSpace(consoleOut))
        {
            embed.AddField(a => a
                .WithName("Console Output")
                .WithValue(Format.Code(consoleOut, "txt").Truncate(MaxFieldValueLength)));
        }

        if (!string.IsNullOrWhiteSpace(result.Exception))
        {
            var formatted = Regex.Replace(result.Exception, "^", "- ", RegexOptions.Multiline);
            embed.AddField(a => a
                .WithName($"Exception: {result.ExceptionType}")
                .WithValue(Format.Code(formatted, "diff").Truncate(MaxFieldValueLength)));
        }

        return embed;
    }

    public async Task<EvaluationResult> EvaluateAsync(Context context, string code)
    {
        var sw = new Stopwatch();

        var console = new StringBuilder();
        await using var writer = new ConsoleLikeStringWriter(console);

        var globals = new Globals(writer, context, _services);
        var execution = new ScriptExecutionContext(code);

        sw.Start();
        var script = CSharpScript.Create(execution.Code, execution.Options, typeof(Globals));

        var compilation = script.GetCompilation();
        var compileTime = sw.Elapsed;

        var compileResult = compilation.GetDiagnostics();
        var compileErrors = compileResult
            .Where(a => a.Severity is DiagnosticSeverity.Error)
            .ToImmutableArray();

        if (!compileErrors.IsEmpty)
            return EvaluationResult.CreateErrorResult(code, console.ToString(), sw.Elapsed, compileErrors);

        try
        {
            var state = await script.RunAsync(globals, _ => true).ConfigureAwait(false);
            var result = new EvaluationResult(code, state, console.ToString(), sw.Elapsed, compileTime);
            sw.Stop();

            try
            {
                // Check if the result is serializable, if not return the exception as a result
                _ = JsonSerializer.Serialize(result, EvaluationResult.SerializerOptions);
            }
            catch (Exception ex)
            {
                var exception = $"An exception occurred when serializing: {ex.GetType().Name}: {ex.Message}";
                result = new EvaluationResult
                {
                    Code          = code,
                    CompileTime   = compileTime,
                    ConsoleOut    = console.ToString(),
                    ExecutionTime = sw.Elapsed,
                    Exception     = exception,
                    ExceptionType = ex.GetType().Name
                };
            }

            return result;
        }
        catch (CompilationErrorException ex)
        {
            return EvaluationResult.CreateErrorResult(code, console.ToString(), sw.Elapsed, ex.Diagnostics);
        }
    }
}