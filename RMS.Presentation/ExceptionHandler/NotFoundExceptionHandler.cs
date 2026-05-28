

using Microsoft.AspNetCore.Diagnostics;
using RMS.Application.Exceptions;

namespace RMS.Presentation.ExceptionHandler
{
    public class NotFoundExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<NotFoundExceptionHandler> _logger;

        public NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not NotFoundException notFoundException)
                return false;

            _logger.LogError(notFoundException, "NotFoundException occurred: {Message}", notFoundException.Message);

            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                StatusCode = 404,
                Message = notFoundException.Message
            }, cancellationToken);

            return true;
        }
    }
}
