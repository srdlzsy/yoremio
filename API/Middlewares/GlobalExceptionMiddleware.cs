using Application.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;

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

            var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
            var error = feature.Error;

            var (statusCode, title, exposeMessage) = error switch
            {
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Yetkisiz işlem", true),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Kayıt bulunamadı", true),
                ArgumentException => (StatusCodes.Status400BadRequest, "Geçersiz istek", true),
                _ => (StatusCodes.Status500InternalServerError, "Sunucu hatası", false)
            };

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(error, "Istek islenirken beklenmeyen hata olustu. Yol: {Path}, TraceId: {TraceId}", context.Request.Path, context.TraceIdentifier);
            }
            else
            {
                logger.LogWarning(error, "Istek is kurali hatasiyla sonuclandi. Yol: {Path}, TraceId: {TraceId}", context.Request.Path, context.TraceIdentifier);
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var message = exposeMessage || environment.IsDevelopment()
                ? $"{title}: {error.Message}"
                : title;

            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                message,
                traceId: context.TraceIdentifier));
        }
    }
}
