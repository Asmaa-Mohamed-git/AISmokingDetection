using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AISmokingDetection.Common
{
    /// <summary>
    /// Maps domain exceptions to ProblemDetails responses so callers (including the AI
    /// service's retry logic) get meaningful status codes instead of a blanket 500.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problem = new ProblemDetails { Instance = httpContext.Request.Path };

            switch (exception)
            {
                case ValidationFailedException vex:
                    problem.Status = StatusCodes.Status422UnprocessableEntity;
                    problem.Title = "Validation failed";
                    problem.Extensions["errors"] = vex.Errors;
                    _logger.LogWarning("Validation failed on {Path}: {Errors}",
                        httpContext.Request.Path, string.Join("; ", vex.Errors));
                    break;

                case KeyNotFoundException:
                    problem.Status = StatusCodes.Status404NotFound;
                    problem.Title = "Not found";
                    problem.Detail = exception.Message;
                    break;

                case InvalidOperationException:
                    problem.Status = StatusCodes.Status409Conflict;
                    problem.Title = "Conflict";
                    problem.Detail = exception.Message;
                    _logger.LogWarning("Conflict on {Path}: {Message}",
                        httpContext.Request.Path, exception.Message);
                    break;

                case ArgumentException:
                    problem.Status = StatusCodes.Status400BadRequest;
                    problem.Title = "Bad request";
                    problem.Detail = exception.Message;
                    break;

                case BadHttpRequestException bad:
                    problem.Status = bad.StatusCode;
                    problem.Title = "Bad request";
                    break;

                default:
                    _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
                    problem.Status = StatusCodes.Status500InternalServerError;
                    problem.Title = "An unexpected error occurred";
                    break; // no internal details returned
            }

            httpContext.Response.StatusCode = problem.Status!.Value;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }
    }
}
