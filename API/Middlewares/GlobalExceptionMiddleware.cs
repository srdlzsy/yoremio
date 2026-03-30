using Application.DTOs;
using Microsoft.AspNetCore.Diagnostics;

namespace API.Middlewares
{
    public static class GlobalExceptionMiddleware
    {
        public static async Task HandleAsync(HttpContext context)
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("GlobalExceptionMiddleware");

            if (feature?.Error is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
            }

            var (statusCode, title) = feature.Error switch
            {
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Yetkisiz işlem"),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Kayıt bulunamadı"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Geçersiz istek"),
                _ => (StatusCodes.Status500InternalServerError, "Sunucu hatası")
            };

            logger.LogError(feature.Error, "İstek işlenirken hata oluştu. Yol: {Path}, TraceId: {TraceId}", context.Request.Path, context.TraceIdentifier);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                $"{title}: {feature.Error.Message}",
                traceId: context.TraceIdentifier));
        }
    }
}
