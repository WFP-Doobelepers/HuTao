using HuTao.Services.Core.Listeners;
using Xunit;

namespace HuTao.Tests.Services.Core;

public class UserFacingErrorTests
{
    [Fact]
    public void Exception_ShowsOnlyAnIdAndNeverTheExceptionText()
    {
        var (message, id) = UserFacingError.Describe(true, "Exception", "Npgsql: relation \"Users\" does not exist");

        Assert.NotNull(id);
        Assert.Contains(id, message);
        Assert.DoesNotContain("Npgsql", message);
        Assert.DoesNotContain("relation", message);
    }

    [Fact]
    public void Exceptions_GetDifferentIds()
    {
        var first = UserFacingError.Describe(true, "Exception", "a").Id;
        var second = UserFacingError.Describe(true, "Exception", "a").Id;

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ParseAndPreconditionErrors_KeepTheirReason()
    {
        var (message, id) = UserFacingError.Describe(false, "UnmetPrecondition", "You need Manage Messages.");

        Assert.Null(id);
        Assert.Equal("UnmetPrecondition: You need Manage Messages.", message);
    }
}
