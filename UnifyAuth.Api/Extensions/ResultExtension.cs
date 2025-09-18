using Application.Common.Results.Concrete;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UnifyAuth.Api.Extensions
{
    public static class ResultExtension
    {
        private static IHttpContextAccessor _http;
        public static void Configure(IHttpContextAccessor accessor) => _http = accessor;

        public static IResult ToProblemDetails(this Application.Common.Results.Abstracts.IResult result)
        {
            if (result.Success)
                throw new InvalidOperationException("Cannot convert a successful result to ProblemDetails.");

            var traceId = _http?.HttpContext?.TraceIdentifier;
            var ext = new Dictionary<string, object?>
            {
                ["traceId"] = traceId,
            };

            string[]? errors = null;
            if (result.Messages is { Length: > 0 })
                errors = result.Messages.Where(m => !string.IsNullOrWhiteSpace(m)).ToArray();
            else if (!string.IsNullOrWhiteSpace(result.Message))
                errors = new[] { result.Message };
            
            if (errors is { Length: > 0 })
                ext["errors"] = errors; 

            return Results.Problem(
                statusCode: GetStatusCode(result.ErrorType!),
                title: result.ErrorType?.Code,
                detail: errors?.Length == 1 ? errors[0] : "More than one error occurred",
                type: GetType(result.ErrorType!),
                extensions: ext);

            static int GetStatusCode(AppError errorType) =>
                errorType.Code switch
                {
                    "NotFound" => StatusCodes.Status404NotFound,
                    "Validation" => StatusCodes.Status400BadRequest,
                    "BadRequest" => StatusCodes.Status400BadRequest,
                    "ConcurrencyFailure" => StatusCodes.Status409Conflict,
                    "Unauthorized" => StatusCodes.Status401Unauthorized,
                    _ => StatusCodes.Status500InternalServerError
                };

            static string GetType(AppError errorType) =>
                errorType.Code switch
                {
                    "NotFound" => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                    "Validation" => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                    "BadRequest" => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                    "ConcurrencyFailure" => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
                    "Unauthorized" => "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
                    _ => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
                };
        }
    }
}
