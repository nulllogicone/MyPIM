using Azure.Messaging.EventGrid;
using Azure.Identity;
using MyPIM.Data;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace MyPIM.Services;

public interface IEventService
{
    Task PublishRequestCreatedAsync(AccessRequest request);
    Task PublishRequestRemovedAsync(AccessRequest request);
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
            _logger.LogError(ex, "Failed to publish RequestCreated event");
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
            _logger.LogError(ex, "Failed to publish RequestRemoved event");
        }
    }
}
