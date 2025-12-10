using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Azure.Core;
using Azure;
using MyPIM.Data;

namespace MyPIM.Services;

public class AzureRbacGraphService : IGraphService
{
    private readonly ILogger<AzureRbacGraphService> _logger;
    private readonly PimTableService _pimTableService;
    private readonly ArmClient _armClient;
    private readonly Microsoft.Graph.GraphServiceClient _graphClient;

    public AzureRbacGraphService(ILogger<AzureRbacGraphService> logger, PimTableService pimTableService, IConfiguration configuration)
    {
        _logger = logger;
        _pimTableService = pimTableService;

        var options = new DefaultAzureCredentialOptions();
        
        // DefaultAzureCredential does NOT read UserSecrets/appsettings automatically.
        // We must manually pass the TenantId from configuration.
        var tenantId = configuration["AzureAd:TenantId"];
        if (!string.IsNullOrEmpty(tenantId) && !string.Equals(tenantId, "common", StringComparison.OrdinalIgnoreCase))
        {
            options.TenantId = tenantId;
            options.VisualStudioTenantId = tenantId;
            options.SharedTokenCacheTenantId = tenantId;
            _logger.LogInformation($"[Auth] Configured DefaultAzureCredential with TenantId: {tenantId}");
        }

        var credential = new DefaultAzureCredential(options);
        
        // Validating Identity matches Scope
        // We can't easily peek "who" without getting a token.
        // The ArmClient will handle this, but let's log the credential type if possible or just rely on the logger below.
        
        _armClient = new ArmClient(credential);
        _graphClient = new Microsoft.Graph.GraphServiceClient(credential);
    }

    public async Task<List<PimRoleConfiguration>> GetAvailableDirectoryRolesAsync()
    {
        _logger.LogInformation("Fetching available roles from Table Storage");
        return await _pimTableService.GetConfigurationsAsync();
    }

    public async Task AssignRoleAsync(string userId, string roleId)
    {
        var config = await _pimTableService.GetConfigurationAsync(roleId);
        if (config == null) throw new Exception($"Role Configuration for {roleId} not found");

        if (!Guid.TryParse(userId, out var userObjectId))
        {
            throw new ArgumentException($"User ID '{userId}' is not a valid GUID (Object ID). Cannot assign Azure Role.");
        }

        _logger.LogInformation($"[Auth] Attempting role assignment...");
        _logger.LogInformation($"[Auth] Target Scope: {config.TargetScope}");
        _logger.LogInformation($"[Auth] Role ID: {roleId}");
        _logger.LogInformation($"[Auth] User Object ID: {userId}");

        var scopeId = new ResourceIdentifier(config.TargetScope);
        var roleAssignments = _armClient.GetRoleAssignments(scopeId);
        
        // Create Role Assignment
        // Name must be a GUID
        var assignmentName = Guid.NewGuid().ToString();
        var content = new RoleAssignmentCreateOrUpdateContent(
            new ResourceIdentifier($"/providers/Microsoft.Authorization/roleDefinitions/{roleId}"),
            Guid.Parse(userId)
        )
        {
            PrincipalType = RoleManagementPrincipalType.User 
        };

        await roleAssignments.CreateOrUpdateAsync(WaitUntil.Completed, assignmentName, content);
        _logger.LogInformation("Role assigned successfully");
    }

    public async Task RevokeRoleAsync(string userId, string roleId)
    {
        var config = await _pimTableService.GetConfigurationAsync(roleId);
        if (config == null) throw new Exception($"Role Configuration for {roleId} not found");

        _logger.LogInformation($"Revoking Role {config.RoleName} ({roleId}) from User {userId} on scope {config.TargetScope}");
        
        var scopeId = new ResourceIdentifier(config.TargetScope);
        var roleAssignments = _armClient.GetRoleAssignments(scopeId);

        await foreach (var assignment in roleAssignments.GetAllAsync())
        {
            if (assignment.Data.PrincipalId == Guid.Parse(userId) && 
                assignment.Data.RoleDefinitionId.Name.Equals(roleId, StringComparison.OrdinalIgnoreCase))
            {
                await assignment.DeleteAsync(WaitUntil.Completed);
                _logger.LogInformation($"Deleted assignment {assignment.Data.Name}");
                return;
            }
        }
        
        _logger.LogWarning("Role assignment not found to revoke");
    }

    public async Task<string> GetUserDisplayNameAsync(string userId)
    {
        try
        {
            var user = await _graphClient.Users[userId].GetAsync();
            return user?.DisplayName ?? $"User {userId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching user {userId}");
            return $"User {userId}";
        }
    }
}
