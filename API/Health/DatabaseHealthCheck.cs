using Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.Health
{
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly YoremioContext _context;

        public DatabaseHealthCheck(YoremioContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? HealthCheckResult.Healthy("PostgreSQL baglantisi hazir.")
                    : HealthCheckResult.Unhealthy("PostgreSQL baglantisi kurulamadi.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("PostgreSQL baglantisi hatali.", ex);
            }
        }
    }
}
