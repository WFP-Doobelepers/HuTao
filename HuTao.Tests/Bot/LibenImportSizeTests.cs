using HuTao.Bot.Modules;
using Xunit;

namespace HuTao.Tests.Bot;

public class LibenImportSizeTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1024 * 1024, true)]
    [InlineData(1024 * 1024 + 1, false)]
    [InlineData(-1, false)]
    public void Import_IsCappedAtOneMegabyte(int size, bool allowed)
        => Assert.Equal(allowed, LibenModule.IsImportSizeAllowed(size));
}
