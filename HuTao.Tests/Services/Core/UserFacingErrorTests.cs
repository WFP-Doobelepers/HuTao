using System;
using System.Net;
using System.Reflection;
using Discord;
using Discord.Net;
using HuTao.Services.Core.Listeners;
using Moq;
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

    // A command that refuses on purpose says why, as it did before errors got IDs.
    [Fact]
    public void UserFacingException_ShowsItsMessage()
    {
        var error = new UserFacingException("Channel is already at the top of the category.");

        var (message, id) = UserFacingError.Describe(true, "Exception", error.Message, error);

        Assert.Null(id);
        Assert.Equal("Channel is already at the top of the category.", message);
    }

    // Discord's refusal names nothing inside the bot, and staff need it to fix a role order or a permission.
    [Fact]
    public void DiscordRefusal_ShowsDiscordsReason()
    {
        var error = new HttpException(HttpStatusCode.Forbidden, Mock.Of<IRequest>(), DiscordErrorCode.MissingPermissions,
            "Missing Permissions", null);

        var (message, id) = UserFacingError.Describe(true, "Exception", error.Message, error);

        Assert.Null(id);
        Assert.Equal("Discord refused the request: Missing Permissions", message);
    }

    [Fact]
    public void UserFacingException_InsideAnInvocation_IsStillShown()
    {
        var error = new TargetInvocationException(new UserFacingException("A command with that name already exists."));

        Assert.Equal("A command with that name already exists.", UserFacingError.Describe(true, "Exception", "", error).Message);
    }

    [Fact]
    public void AnyOtherException_StillShowsOnlyAnId()
    {
        var error = new InvalidOperationException("Sequence contains no elements");

        var (message, id) = UserFacingError.Describe(true, "Exception", error.Message, error);

        Assert.NotNull(id);
        Assert.DoesNotContain("Sequence", message);
    }
}
