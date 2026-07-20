using System.Text.Json;
using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Domain.Exceptions;

namespace DigitalBookLibrary.WebAPI.Middleware
{
    /// <summary>
    /// The single place that turns any exception into a uniform <see cref="ApiResponse{T}"/> and the
    /// correct HTTP status. Maps exception TYPE → status (HTTP knowledge lives only here), logs the
    /// technical description, and returns codes only.
    /// </summary>
    public sealed class GlobalExceptionMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
            catch (ValidationAppException ex)
            {
                _logger.LogWarning("Validation failed: {Codes}", string.Join(",", ex.Errors.Select(e => e.Code)));
                await WriteAsync(context, StatusCodes.Status400BadRequest,
                    ApiResponse.Fail(ResponseCodes.ValidationFailed, ex.Errors.Select(e => e.Code).ToArray()));
            }
            catch (AppException ex)
            {
                _logger.LogWarning("Handled {Type} {Code}: {Description}",
                    ex.GetType().Name, ex.Error.Code, ex.Error.Description);
                await WriteAsync(context, MapStatus(ex),
                    ApiResponse.Fail(ResponseCodes.OperationFailed, ex.Error.Code));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteAsync(context, StatusCodes.Status500InternalServerError,
                    ApiResponse.Fail(ResponseCodes.InternalServerError, ResponseCodes.InternalServerError));
            }
        }

        // Exception TYPE -> HTTP status. HTTP knowledge is confined to the WebAPI layer.
        private static int MapStatus(AppException ex) => ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedAppException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ValidationAppException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        private static async Task WriteAsync(HttpContext context, int statusCode, object body)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }
    }
}
