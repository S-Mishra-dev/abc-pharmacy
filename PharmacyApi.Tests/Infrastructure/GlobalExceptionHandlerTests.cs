using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PharmacyApi.Infrastructure;

namespace PharmacyApi.Tests.Infrastructure;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ArgumentException_Writes400ProblemDetails()
    {
        ProblemDetails? written = null;
        GlobalExceptionHandler handler = CreateHandler(ctx => written = ctx.ProblemDetails);
        DefaultHttpContext httpContext = CreateHttpContext();

        bool handled = await handler.TryHandleAsync(
            httpContext,
            new ArgumentException("Price must be positive."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.NotNull(written);
        Assert.Equal(StatusCodes.Status400BadRequest, written.Status);
        Assert.Equal("Bad Request", written.Title);
        Assert.Equal("Price must be positive.", written.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_UnhandledException_WritesGeneric500ProblemDetails()
    {
        ProblemDetails? written = null;
        GlobalExceptionHandler handler = CreateHandler(ctx => written = ctx.ProblemDetails);
        DefaultHttpContext httpContext = CreateHttpContext();

        bool handled = await handler.TryHandleAsync(
            httpContext,
            new Exception("Secret connection string leaked"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.NotNull(written);
        Assert.Equal(StatusCodes.Status500InternalServerError, written.Status);
        Assert.Equal("An unexpected error occurred. Please try again later.", written.Detail);
        Assert.DoesNotContain("Secret", written.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryHandleAsync_InvalidOperationException_Writes409ProblemDetails()
    {
        ProblemDetails? written = null;
        GlobalExceptionHandler handler = CreateHandler(ctx => written = ctx.ProblemDetails);
        DefaultHttpContext httpContext = CreateHttpContext();
        const string message = "Insufficient stock. Available: 2, requested: 10.";

        bool handled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException(message),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.NotNull(written);
        Assert.Equal(StatusCodes.Status409Conflict, written.Status);
        Assert.Equal(message, written.Detail);
    }

    private static GlobalExceptionHandler CreateHandler(Action<ProblemDetailsContext> onWrite)
    {
        Mock<IProblemDetailsService> problemDetailsService = new(MockBehavior.Strict);
        problemDetailsService
            .Setup(s => s.WriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback(onWrite)
            .ReturnsAsync(true);

        return new GlobalExceptionHandler(
            Mock.Of<ILogger<GlobalExceptionHandler>>(),
            problemDetailsService.Object);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Path = "/api/medicines"
            }
        };
    }
}
