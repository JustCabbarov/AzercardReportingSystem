using Microsoft.AspNetCore.Diagnostics;

namespace RMS.Presentation.ExceptionHandler
{
    public class GlobalExceptionHandler : IExceptionHandler
    {

        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(new
            {
                StatusCode = 500,
                Message = exception.Message
            }, cancellationToken);

            return true;
        }
    }
}
