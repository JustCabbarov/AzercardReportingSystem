using Microsoft.AspNetCore.Diagnostics;
using RMS.Application.Exceptions;

namespace Presentation.ExceptionHandler
{
    public class UnauthorizedExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<UnauthorizedExceptionHandler> _logger;

        public UnauthorizedExceptionHandler(ILogger<UnauthorizedExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not UnauthorizedException unauthorizedException)
                return false;

            _logger.LogError(unauthorizedException, "UnauthorizedException occurred: {Message}", unauthorizedException.Message);

            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                StatusCode = 401,
                Message = unauthorizedException.Message
            }, cancellationToken);

            return true;
        }
    }
}