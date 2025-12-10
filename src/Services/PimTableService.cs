using Azure;
using Azure.Data.Tables;
using MyPIM.Data;

namespace MyPIM.Services;

public class PimTableService
{
    private readonly TableClient _configTable;
    private readonly TableClient _requestsTable;
    private readonly IEventService _eventService;

    public PimTableService(IConfiguration configuration, IEventService eventService)
    {
        _eventService = eventService;
        var connectionString = configuration.GetConnectionString("AzureTableStorage")
            ?? throw new InvalidOperationException("Connection string 'AzureTableStorage' not found.");

        var serviceClient = new TableServiceClient(connectionString);

        _configTable = serviceClient.GetTableClient("PimConfiguration");
        _requestsTable = serviceClient.GetTableClient("PimRequests");

        _configTable.CreateIfNotExists();
        _requestsTable.CreateIfNotExists();
    }

    // --- Configuration ---
    public async Task<List<PimRoleConfiguration>> GetConfigurationsAsync()
    {
        var query = _configTable.QueryAsync<PimRoleConfiguration>(filter: $"PartitionKey eq 'CONFIG'");
        var results = new List<PimRoleConfiguration>();
        await foreach (var page in query.AsPages())
        {
            results.AddRange(page.Values);
        }
        return results;
    }

    public async Task SaveConfigurationAsync(PimRoleConfiguration config)
    {
        config.PartitionKey = "CONFIG"; // Ensure PK
        await _configTable.UpsertEntityAsync(config);
    }

    public async Task DeleteConfigurationAsync(string roleId)
    {
        await _configTable.DeleteEntityAsync("CONFIG", roleId);
    }

    public async Task<PimRoleConfiguration?> GetConfigurationAsync(string roleId)
    {
        try
        {
            return await _configTable.GetEntityAsync<PimRoleConfiguration>("CONFIG", roleId);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    // --- Requests ---
    public async Task<List<AccessRequest>> GetRequestsByStatusAsync(string status)
    {
        var query = _requestsTable.QueryAsync<AccessRequest>(filter: $"PartitionKey eq '{status}'");
        var results = new List<AccessRequest>();
        await foreach (var page in query.AsPages())
        {
            results.AddRange(page.Values);
        }
        return results;
    }

    public async Task<List<AccessRequest>> GetUserRequestsAsync(string userId)
    {
        // Cross-partition query: acceptable for low volume (scan).
        // For higher volume, we'd need a secondary index table.
        var query = _requestsTable.QueryAsync<AccessRequest>(filter: $"UserId eq '{userId}'");
        var results = new List<AccessRequest>();
        await foreach (var page in query.AsPages())
        {
            results.AddRange(page.Values);
        }
        return results;
    }

    public async Task SubmitRequestAsync(AccessRequest request)
    {
        request.PartitionKey = RequestStatus.Pending;
        await _requestsTable.AddEntityAsync(request);
        await _eventService.PublishRequestCreatedAsync(request);
    }

    public async Task DeleteRequestAsync(AccessRequest request)
    {
        await _requestsTable.DeleteEntityAsync(request.PartitionKey, request.RowKey, request.ETag);
        await _eventService.PublishRequestRemovedAsync(request);
    }

    public async Task UpdateRequestStatusAsync(AccessRequest request, string newStatus)
    {
        // Copy to new partition and delete from old
        var oldPk = request.PartitionKey;

        request.PartitionKey = newStatus;

        // Transaction: Add New, Delete Old
        var batch = new List<TableTransactionAction>
        {
            new TableTransactionAction(TableTransactionActionType.Add, request),
            new TableTransactionAction(TableTransactionActionType.Delete, new AccessRequest { PartitionKey = oldPk, RowKey = request.RowKey, ETag = request.ETag })
        };

        if (oldPk != newStatus)
        {
            // Cross-partition update: Batch not supported if partitions differ
            // 1. Add new
            await _requestsTable.AddEntityAsync(request);
            // 2. Delete old
            await _requestsTable.DeleteEntityAsync(oldPk, request.RowKey, request.ETag);
        }
        else
        {
            await _requestsTable.UpdateEntityAsync(request, request.ETag);
        }
    }
}
