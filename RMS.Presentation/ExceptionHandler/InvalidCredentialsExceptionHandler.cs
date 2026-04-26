using Microsoft.AspNetCore.Diagnostics;
using RMS.Application.Exceptions;

namespace RMS.Presentation.ExceptionHandler
{
    // Presentation/ExceptionHandler/InvalidCredentialsExceptionHandler.cs
    public class InvalidCredentialsExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not InvalidCredentialsException invalidCredentialsException)
                return false;

            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                StatusCode = 401,
                Message = invalidCredentialsException.Message
            }, cancellationToken);

            return true;
        }
    }
}
