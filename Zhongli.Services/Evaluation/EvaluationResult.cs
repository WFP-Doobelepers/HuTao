using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Zhongli.Services.Evaluation;

public class EvaluationResult
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        MaxDepth                    = 10240,
        IncludeFields               = true,
        PropertyNameCaseInsensitive = true,
        WriteIndented               = true,
        ReferenceHandler            = ReferenceHandler.IgnoreCycles
    };

    public EvaluationResult() { }

    public EvaluationResult(string code, ScriptState<object?> state, string consoleOut, TimeSpan executionTime,
        TimeSpan compileTime)
    {
        state = state ?? throw new ArgumentNullException(nameof(state));

        ReturnValue = state.ReturnValue;
        var type = state.ReturnValue?.GetType();

        if (type?.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEnumerator)) ?? false)
        {
            var genericParams = type.GetGenericArguments();

            if (genericParams.Length == 2)
            {
                type = typeof(List<>).MakeGenericType(genericParams[1]);

                ReturnValue = Activator.CreateInstance(type, ReturnValue);
            }
        }

        ReturnTypeName = type?.ParseGenericArgs();
        ExecutionTime  = executionTime;
        CompileTime    = compileTime;
        ConsoleOut     = consoleOut;
        Code           = code;
        Exception      = state.Exception?.Message;
        ExceptionType  = state.Exception?.GetType().Name;
    }

    public object? ReturnValue { get; set; }

    public string Code { get; set; } = null!;

    public string ConsoleOut { get; set; } = null!;

    public string? Exception { get; set; }

    public string? ExceptionType { get; set; }

    public string? ReturnTypeName { get; set; }

    public TimeSpan CompileTime { get; set; }

    public TimeSpan ExecutionTime { get; set; }

    public static EvaluationResult CreateErrorResult(string code, string consoleOut, TimeSpan compileTime,
        ImmutableArray<Diagnostic> compileErrors)
    {
        var ex = new CompilationErrorException(string.Join("\n", compileErrors.Select(a => a.GetMessage())),
            compileErrors);
        var errorResult = new EvaluationResult
        {
            Code           = code,
            CompileTime    = compileTime,
            ConsoleOut     = consoleOut,
            Exception      = ex.Message,
            ExceptionType  = ex.GetType().Name,
            ExecutionTime  = TimeSpan.FromMilliseconds(0),
            ReturnValue    = null,
            ReturnTypeName = null
        };
        return errorResult;
    }
}