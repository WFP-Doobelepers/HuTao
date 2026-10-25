using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HuTao.Data;
using HuTao.Services.Image;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using Xunit;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace HuTao.Tests.Services.Image;

/// <summary>
///     ImageSharp 3.1.12 has five open advisories, and the releases that fix them need a Six Labors license key to build
///     in Release. Hu Tao keeps 3.1.12 because its code reaches none of the five: it never encodes TIFF (GHSA-j9gm-c75j-xc9q
///     and GHSA-jjfr-hcj7-qf5w), never equalizes a histogram (GHSA-j3p4-wp97-rph4) and never reads
///     <see cref="IccProfile.Entries" /> (GHSA-gwg2-r3hj-4w44). This test reads the IL of every method in the bot, the
///     services and the data project, and fails when one of them starts to.
/// </summary>
public class ImageSharpAdvisoryGuardTests
{
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
        | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode) f.GetValue(null)!)
        .ToDictionary(o => o.Value);

    [Fact]
    public void NoCode_EncodesTiff_EqualizesAHistogram_OrReadsIccEntries()
    {
        Assembly[] assemblies = [typeof(HuTao.Bot.Bot).Assembly, typeof(ImageService).Assembly, typeof(HuTaoContext).Assembly];

        var offenders = assemblies
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => t.GetMethods(All).Cast<MethodBase>().Concat(t.GetConstructors(All)))
            .SelectMany(m => References(m).Select(r => (Method: m, Reference: r, Path: Path(r))))
            .Where(x => x.Path is not null)
            .Select(x => $"{x.Method.DeclaringType?.FullName}.{x.Method.Name} {x.Path} ({x.Reference.DeclaringType?.Name}.{x.Reference.Name})")
            .Distinct()
            .ToList();

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }

    private static string? Path(MemberInfo member)
    {
        var type = member as Type ?? member.DeclaringType;
        if (type?.Assembly != typeof(ImageSharpImage).Assembly) return null;

        if (type.Namespace?.StartsWith("SixLabors.ImageSharp.Formats.Tiff") == true || member.Name.StartsWith("SaveAsTiff"))
            return "uses ImageSharp's TIFF format";

        // These overloads take the encoder from a file name or from the format the image was decoded from, so they
        // write a decoded TIFF back as TIFF.
        if (member is MethodBase { Name: "Save" or "SaveAsync" } save
            && save.GetParameters().Any(p => p.ParameterType == typeof(string)
                || typeof(IImageFormat).IsAssignableFrom(p.ParameterType)))
            return "saves in a format chosen when it runs";

        if (member is MethodBase { Name: nameof(ImageFormatManager.GetEncoder) } && type == typeof(ImageFormatManager))
            return "looks an encoder up by format";

        if (type.Namespace == "SixLabors.ImageSharp.Processing.Processors.Normalization"
            || member.Name == "HistogramEqualization")
            return "equalizes a histogram";

        if (member is MethodInfo { Name: "get_Entries" } && type == typeof(IccProfile))
            return "reads IccProfile.Entries";

        return null;
    }

    private static IEnumerable<MemberInfo> References(MethodBase method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null) yield break;

        var typeArguments = method.DeclaringType?.IsGenericType == true ? method.DeclaringType.GetGenericArguments() : null;
        var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

        var i = 0;
        while (i < il.Length)
        {
            var value = il[i] == 0xFE ? (short) (0xFE00 | il[i + 1]) : il[i];
            i += il[i] == 0xFE ? 2 : 1;

            var opCode = OpCodesByValue[value];
            var token = opCode.OperandType is OperandType.InlineMethod or OperandType.InlineField
                or OperandType.InlineType or OperandType.InlineTok
                ? BitConverter.ToInt32(il, i)
                : 0;

            i += opCode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
                _ => 4
            };

            if (token != 0)
                yield return method.Module.ResolveMember(token, typeArguments, methodArguments)!;
        }
    }
}
