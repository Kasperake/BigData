using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BigData.API.ExceptionHandler;

public class WeatherExceptionHandler(IHostEnvironment environment, ILogger<WeatherExceptionHandler> logger, IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Detail = environment.IsDevelopment() ? exception.Message : null
            }
        };

        var status = problem.Status ?? StatusCodes.Status500InternalServerError;

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogWarning("{ExceptionType}: {Message}", exception.GetType().Name, exception.Message);

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
