using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace HuTao.Services.Evaluation;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class ConsoleLikeStringWriter(StringBuilder builder) : StringWriter(builder)
{
    public ConsoleKeyInfo ReadKey() => new('z', ConsoleKey.Z, false, false, false);

    public ConsoleKeyInfo ReadKey(bool z)
    {
        if (z) Write("z");
        return ReadKey();
    }

    public int Read() => 0;

    public Stream OpenStandardError() => new MemoryStream();

    public Stream OpenStandardError(int a) => new MemoryStream(a);

    public Stream OpenStandardInput() => new MemoryStream();

    public Stream OpenStandardInput(int a) => new MemoryStream(a);

    public Stream OpenStandardOutput() => new MemoryStream();

    public Stream OpenStandardOutput(int a) => new MemoryStream(a);

    public string ReadLine() => $"{nameof(HuTao)}{Environment.NewLine}";

    public void Beep() { }

    public void Beep(int a, int b) { }

    public void Clear() { }

    public void MoveBufferArea(int a, int b, int c, int d, int e) { }

    public void MoveBufferArea(int a, int b, int c, int d, int e, char f, ConsoleColor g, ConsoleColor h) { }

    public void ResetColor() { }

    public void SetBufferSize(int a, int b) { }

    public void SetCursorPosition(int a, int b) { }

    public void SetError(TextWriter wr) { }

    public void SetIn(TextWriter wr) { }

    public void SetOut(TextWriter wr) { }

    public void SetWindowPosition(int a, int b) { }

    public void SetWindowSize(int a, int b) { }
}