

using Microsoft.AspNetCore.Diagnostics;
using RMS.Application.Exceptions;

namespace RMS.Presentation.ExceptionHandler
{
    public class NullExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<NullExceptionHandler> _logger;

        public NullExceptionHandler(ILogger<NullExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
           if (exception is not NotNullExceptions argumentNullException)
           {
               return  false;
            }
              _logger.LogError(argumentNullException, "ArgumentNullException occurred: {Message}", argumentNullException.Message);
                return true;
        }
    }
}
