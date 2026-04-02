namespace ProductService.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductService.Services;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ProductDbContext _db;

    public DatabaseHealthCheck(ProductDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(ct);
            return canConnect
                ? HealthCheckResult.Healthy("Database is reachable")
                : HealthCheckResult.Unhealthy("Database is unreachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}

public class StartupHealthCheck : IHealthCheck
{
    private volatile bool _isReady = false;

    public bool IsReady { get => _isReady; set => _isReady = value; }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        return Task.FromResult(_isReady
            ? HealthCheckResult.Healthy("Service is ready")
            : HealthCheckResult.Unhealthy("Service is starting up"));
    }
}
