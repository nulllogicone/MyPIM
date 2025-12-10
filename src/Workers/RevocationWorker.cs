using MyPIM.Data;

namespace MyPIM.Services;

public class RevocationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RevocationWorker> _logger;

    public RevocationWorker(IServiceProvider serviceProvider, ILogger<RevocationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RevocationWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var tableService = scope.ServiceProvider.GetRequiredService<PimTableService>();
                    var graphService = scope.ServiceProvider.GetRequiredService<IGraphService>();

                    // 1. Get all Active requests
                    var activeRequests = await tableService.GetRequestsByStatusAsync(RequestStatus.Active);

                    // 2. Filter in-memory for expired ones
                    var expiredRequests = activeRequests
                        .Where(r => r.ExpiresAt <= DateTimeOffset.UtcNow)
                        .ToList();

                    if (expiredRequests.Any())
                    {
                        _logger.LogInformation($"Found {expiredRequests.Count} expired requests.");
                    }

                    foreach (var req in expiredRequests)
                    {
                        try
                        {
                            // 3. Revoke in Graph
                            await graphService.RevokeRoleAsync(req.UserId, req.RoleId);

                            // 4. Update Status in Table (Move active -> Expired)
                            req.RevokedAt = DateTimeOffset.UtcNow;
                            await tableService.UpdateRequestStatusAsync(req, RequestStatus.Expired);

                            _logger.LogInformation($"Revoked access for Request {req.RowKey}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error revoking request {req.RowKey}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RevocationWorker loop");
            }

            // Wait 1 minute before next check
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
