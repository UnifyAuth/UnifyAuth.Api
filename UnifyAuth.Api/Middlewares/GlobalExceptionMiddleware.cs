using Application.Common.Results.Concrete;
using Microsoft.AspNetCore.Diagnostics;

namespace UnifyAuth.Api.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var error = new ErrorResult("An unexpected error occurred. Please try again later.", "SystemError");

            await context.Response.WriteAsJsonAsync(error, cancellationToken);
            return true;
        }
    }
}
