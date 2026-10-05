using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BackendMoviles.Api;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (statusCode, title, detail) = exception switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Solicitud inválida", "Uno o más campos no superaron la validación."),
                ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida", exception.Message),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado", exception.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado", exception.Message),
                InvalidOperationException => (StatusCodes.Status409Conflict, "Conflicto", exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
            };

            if (statusCode >= 500)
            {
                logger.LogError(exception, "Unhandled API exception for {Path}", context.Request.Path);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            if (exception is ValidationException validationException)
            {
                problem.Extensions["errors"] = validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            }

            await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
        }
    }
}