using Board.Application.AppData.Common.Exceptions;
using Board.Contracts;

namespace Board.Host.Api.Middlewares;

/// <summary>
/// Преобразует исключения в ответ с <see cref="ErrorDto"/> и соответствующим HTTP-статусом.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Инициализирует экземпляр <see cref="ExceptionHandlingMiddleware"/>.
    /// </summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Обработать запрос.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Клиент закрыл соединение — отвечать некому.
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (statusCode, errorCode) = exception switch
            {
                EntityNotFoundException => (StatusCodes.Status404NotFound, "not_found"),
                BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "business_rule_violation"),
                AccessDeniedException => (StatusCodes.Status403Forbidden, "forbidden"),
                InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "invalid_credentials"),
                _ => (StatusCodes.Status500InternalServerError, "internal_error")
            };

            string userMessage;
            if (exception is BusinessException)
            {
                _logger.LogInformation("Запрос {Method} {Path} отклонён: {Message}", context.Request.Method, context.Request.Path, exception.Message);
                userMessage = exception.Message;
            }
            else
            {
                _logger.LogError(exception, "Необработанная ошибка при выполнении запроса {Method} {Path}", context.Request.Method, context.Request.Path);
                userMessage = "Произошла внутренняя ошибка.";
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new ErrorDto { ErrorCode = errorCode, UserMessage = userMessage });
        }
    }
}
