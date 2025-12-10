using MyPIM.Data;

namespace MyPIM.Services;

public interface IGraphService
{
    Task<List<PimRoleConfiguration>> GetAvailableDirectoryRolesAsync();
    Task AssignRoleAsync(string userId, string roleId);
    Task RevokeRoleAsync(string userId, string roleId);
    Task<string> GetUserDisplayNameAsync(string userId);
}
