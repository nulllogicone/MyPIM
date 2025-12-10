using MyPIM.Data;

namespace MyPIM.Services;

public interface IGraphService
{
    Task<List<PimRoleConfiguration>> GetAvailableDirectoryRolesAsync();
    Task AssignRoleAsync(string userId, string roleId);
    Task RevokeRoleAsync(string userId, string roleId);
    Task<string> GetUserDisplayNameAsync(string userId);
}

public class MockGraphService : IGraphService
{
    private readonly ILogger<MockGraphService> _logger;

    public MockGraphService(ILogger<MockGraphService> logger)
    {
        _logger = logger;
    }

    public Task<List<PimRoleConfiguration>> GetAvailableDirectoryRolesAsync()
    {
        // Return some fake Azure AD roles
        return Task.FromResult(new List<PimRoleConfiguration>
        {
            new PimRoleConfiguration { RowKey = Guid.NewGuid().ToString(), RoleName = "Global Administrator", DefaultDurationMinutes = 60 },
            new PimRoleConfiguration { RowKey = Guid.NewGuid().ToString(), RoleName = "Exchange Administrator", DefaultDurationMinutes = 120 },
            new PimRoleConfiguration { RowKey = Guid.NewGuid().ToString(), RoleName = "User Administrator", DefaultDurationMinutes = 480 },
            new PimRoleConfiguration { RowKey = Guid.NewGuid().ToString(), RoleName = "Helpdesk Administrator", DefaultDurationMinutes = 60 }
        });
    }

    public Task AssignRoleAsync(string userId, string roleId)
    {
        _logger.LogInformation($"[MOCK GRAPH] Assigned Role {roleId} to User {userId}");
        return Task.CompletedTask;
    }

    public Task RevokeRoleAsync(string userId, string roleId)
    {
        _logger.LogInformation($"[MOCK GRAPH] Revoked Role {roleId} from User {userId}");
        return Task.CompletedTask;
    }

    public Task<string> GetUserDisplayNameAsync(string userId)
    {
        return Task.FromResult($"User {userId.Substring(0, 5)}");
    }
}
