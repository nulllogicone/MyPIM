using Azure.Messaging.EventGrid;
using Azure.Identity;
using MyPIM.Data;

namespace MyPIM.Services;

public interface IEventService
{
    Task PublishRequestCreatedAsync(AccessRequest request);
    Task PublishRequestRemovedAsync(AccessRequest request);
    Task PublishRequestApprovedAsync(AccessRequest request);
    Task PublishRequestRejectedAsync(AccessRequest request);
    Task PublishAppStartedAsync();
}

public class EventGridService : IEventService
{
    private readonly EventGridPublisherClient _client;
    private readonly ILogger<EventGridService> _logger;

    public EventGridService(IConfiguration configuration, ILogger<EventGridService> logger)
    {
        _logger = logger;
        var endpoint = configuration["EventGrid:Endpoint"];
        
        if (string.IsNullOrEmpty(endpoint))
        {
            _logger.LogWarning("EventGrid:Endpoint is missing. Events will not be published.");
            _client = null!;
        }
        else
        {
            // Use DefaultAzureCredential (Managed Identity in Azure, VS Creds locally)
            _client = new EventGridPublisherClient(new Uri(endpoint), new DefaultAzureCredential());
        }
    }

    public async Task PublishRequestCreatedAsync(AccessRequest request)
    {
        if (_client == null) return;

        var evt = new EventGridEvent(
            subject: $"requests/{request.RowKey}",
            eventType: "MyPIM.Request.Created",
            dataVersion: "1.0",
            data: request
        );

        try 
        {
            await _client.SendEventAsync(evt);
            _logger.LogInformation($"Published RequestCreated event for {request.RowKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish RequestCreated event: {ex.Message}");
        }
    }

    public async Task PublishRequestRemovedAsync(AccessRequest request)
    {
        if (_client == null) return;

        var evt = new EventGridEvent(
            subject: $"requests/{request.RowKey}",
            eventType: "MyPIM.Request.Removed",
            dataVersion: "1.0",
            data: request
        );

        try
        {
            await _client.SendEventAsync(evt);
            _logger.LogInformation($"Published RequestRemoved event for {request.RowKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish RequestRemoved event: {ex.Message}");
        }
    }

    public async Task PublishRequestApprovedAsync(AccessRequest request)
    {
        if (_client == null) return;

        var evt = new EventGridEvent(
            subject: $"requests/{request.RowKey}",
            eventType: "MyPIM.Request.Approved",
            dataVersion: "1.0",
            data: request
        );

        try
        {
            await _client.SendEventAsync(evt);
            _logger.LogInformation($"Published RequestApproved event for {request.RowKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish RequestApproved event: {ex.Message}");
        }
    }

    public async Task PublishRequestRejectedAsync(AccessRequest request)
    {
        if (_client == null) return;

        var evt = new EventGridEvent(
            subject: $"requests/{request.RowKey}",
            eventType: "MyPIM.Request.Rejected",
            dataVersion: "1.0",
            data: request
        );

        try
        {
            await _client.SendEventAsync(evt);
            _logger.LogInformation($"Published RequestRejected event for {request.RowKey}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish RequestRejected event: {ex.Message}");
        }
    }

    public async Task PublishAppStartedAsync()
    {
        if (_client == null) return;

        var evt = new EventGridEvent(
            subject: $"app/lifecycle-{Environment.MachineName}",
            eventType: "MyPIM.App.Started",
            dataVersion: "1.0",
            data: new { 
                Timestamp = DateTimeOffset.UtcNow,
                Environment.MachineName,
                Environment.OSVersion,
                Environment.UserName,
                Message = "Application Started" }
        );

        try
        {
            await _client.SendEventAsync(evt);
            _logger.LogInformation("Published AppStarted event");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish AppStarted event: {ex.Message}");
        }
    }
}
