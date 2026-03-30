using Microsoft.Extensions.Primitives;

namespace API.Middlewares
{
    public sealed class RequestContextMiddleware
    {
        private const string CorrelationHeaderName = "X-Correlation-Id";

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestContextMiddleware> _logger;

        public RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Her isteğe iz sürülebilir bir kimlik veriyoruz ki loglar aynı akışta birleşsin.
            var correlationId = ResolveCorrelationId(context);
            context.TraceIdentifier = correlationId;
            context.Response.Headers[CorrelationHeaderName] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await _next(context);
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationHeaderName, out var headerValue) &&
                !StringValues.IsNullOrEmpty(headerValue))
            {
                return headerValue.ToString();
            }

            return Guid.NewGuid().ToString("N");
        }
    }
}
