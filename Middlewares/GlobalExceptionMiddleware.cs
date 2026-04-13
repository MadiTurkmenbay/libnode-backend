using System.Text.Json;
using LibNode.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Middlewares;

/// <summary>
/// Глобальный перехватчик ошибок. Ловит все необработанные исключения и
/// возвращает стандартный ответ (RFC 7807 ProblemDetails).
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Произошла необработанная ошибка при обработке запроса.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail) = exception switch
        {
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Конфликт данных",
                conflict.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера",
                "Произошла непредвиденная ошибка на стороне сервера. Пожалуйста, обратитесь к администратору."
            )
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        var env = context.RequestServices.GetService<IWebHostEnvironment>();
        if (env?.IsDevelopment() == true && statusCode == StatusCodes.Status500InternalServerError)
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions["trace"] = exception.StackTrace;
        }

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
