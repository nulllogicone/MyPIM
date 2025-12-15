using Microsoft.EntityFrameworkCore;
using MyPIM.Data;

namespace MyPIM.Services;

public class PimDataService
{
    private readonly MyPimDbContext _db;
    private readonly IEventService _eventService;
    private readonly ILogger<PimDataService> _logger;

    public PimDataService(MyPimDbContext db, IEventService eventService, ILogger<PimDataService> logger)
    {
        _db = db;
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<List<PimRoleConfiguration>> GetConfigurationsAsync()
    {
        var roles = await _db.Roles.Include(r => r.Scope).ToListAsync();
        return roles.Select(r => new PimRoleConfiguration
        {
            PartitionKey = "CONFIG",
            RowKey = r.Id.ToString(),
            RoleName = r.Name,
            TargetScope = r.Scope?.ArmScope ?? string.Empty,
            DefaultDurationMinutes = 60,
            IsEnabled = true
        }).ToList();
    }

    public async Task<PimRoleConfiguration?> GetConfigurationAsync(string roleId)
    {
        if (!Guid.TryParse(roleId, out var roleGuid)) return null;
        var role = await _db.Roles.Include(r => r.Scope).FirstOrDefaultAsync(r => r.Id == roleGuid);
        if (role == null) return null;

        return new PimRoleConfiguration
        {
            PartitionKey = "CONFIG",
            RowKey = role.Id.ToString(),
            RoleName = role.Name,
            TargetScope = role.Scope?.ArmScope ?? string.Empty,
            DefaultDurationMinutes = 60,
            IsEnabled = true
        };
    }

    public async Task SaveConfigurationAsync(PimRoleConfiguration config)
    {
        if (!Guid.TryParse(config.RowKey, out var roleId)) return;

        var scope = await _db.Scopes.FirstOrDefaultAsync(s => s.ArmScope == config.TargetScope);
        if (scope == null)
        {
            scope = new Scope { Id = Guid.NewGuid(), ArmScope = config.TargetScope };
            _db.Scopes.Add(scope);
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role == null)
        {
            role = new Role { Id = roleId, Name = config.RoleName, ScopeId = scope.Id };
            _db.Roles.Add(role);
        }
        else
        {
            role.Name = config.RoleName;
            role.ScopeId = scope.Id;
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteConfigurationAsync(string roleId)
    {
        if (!Guid.TryParse(roleId, out var roleGuid)) return;
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleGuid);
        if (role != null)
        {
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<AccessRequest>> GetRequestsByStatusAsync(string status)
    {
        var state = MapStringToState(status);
        var requests = await _db.Requests
            .Include(r => r.RequestedByUser)
            .Include(r => r.Role)
            .Where(r => r.Status == state)
            .ToListAsync();

        return requests.Select(MapToAccessRequest).ToList();
    }

    public async Task<List<AccessRequest>> GetUserRequestsAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.ObjectId.ToString() == userId);
            if (user == null) return new List<AccessRequest>();
            userGuid = user.Id;
        }

        var requests = await _db.Requests
            .Include(r => r.RequestedByUser)
            .Include(r => r.Role)
            .Where(r => r.RequestedByUserId == userGuid || r.RequestedByUser!.ObjectId.ToString() == userId)
            .ToListAsync();

        return requests.Select(MapToAccessRequest).ToList();
    }

    public async Task SubmitRequestAsync(AccessRequest request)
    {
        if (!Guid.TryParse(request.UserId, out var userObjectId)) return;
        if (!Guid.TryParse(request.RoleId, out var roleGuid)) return;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.ObjectId == userObjectId);
        if (user == null)
        {
            user = new User { Id = Guid.NewGuid(), ObjectId = userObjectId, DisplayName = request.UserDisplayName };
            _db.Users.Add(user);
        }

        var sqlRequest = new Request
        {
            Id = Guid.Parse(request.RowKey),
            RequestedByUserId = user.Id,
            RoleId = roleGuid,
            CreatedAtUtc = request.RequestedAt,
            ExpiresAtUtc = request.ExpiresAt,
            Status = RequestState.Requested
        };

        _db.Requests.Add(sqlRequest);
        await _db.SaveChangesAsync();
        await _eventService.PublishRequestCreatedAsync(request);
    }

    public async Task DeleteRequestAsync(AccessRequest request)
    {
        if (!Guid.TryParse(request.RowKey, out var requestGuid)) return;

        var sqlRequest = await _db.Requests.FirstOrDefaultAsync(r => r.Id == requestGuid);
        if (sqlRequest != null)
        {
            _db.Requests.Remove(sqlRequest);
            await _db.SaveChangesAsync();
            await _eventService.PublishRequestRemovedAsync(request);
        }
    }

    public async Task UpdateRequestStatusAsync(AccessRequest request, string newStatus)
    {
        if (!Guid.TryParse(request.RowKey, out var requestGuid)) return;

        var sqlRequest = await _db.Requests.FirstOrDefaultAsync(r => r.Id == requestGuid);
        if (sqlRequest == null) return;

        var newState = MapStringToState(newStatus);
        sqlRequest.Status = newState;

        if (newStatus == RequestStatus.Active)
        {
            sqlRequest.ActivatedAtUtc = DateTimeOffset.UtcNow;
        }
        else if (newStatus == RequestStatus.Expired || newStatus == RequestStatus.Revoked)
        {
            sqlRequest.RevokedAtUtc = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    private AccessRequest MapToAccessRequest(Request r)
    {
        var statusString = MapStateToString(r.Status);
        return new AccessRequest
        {
            PartitionKey = statusString,
            RowKey = r.Id.ToString(),
            UserId = r.RequestedByUser?.ObjectId.ToString() ?? r.RequestedByUserId.ToString(),
            UserDisplayName = r.RequestedByUser?.DisplayName ?? string.Empty,
            RoleId = r.RoleId.ToString(),
            RoleName = r.Role?.Name ?? string.Empty,
            RequestedAt = r.CreatedAtUtc,
            ApprovedAt = r.ApprovedAtUtc,
            ExpiresAt = r.ExpiresAtUtc,
            RevokedAt = r.RevokedAtUtc
        };
    }

    private RequestState MapStringToState(string status) => status switch
    {
        RequestStatus.Pending => RequestState.Requested,
        RequestStatus.Active => RequestState.Active,
        RequestStatus.Expired => RequestState.Expired,
        RequestStatus.Rejected => RequestState.Revoked,
        RequestStatus.Revoked => RequestState.Revoked,
        _ => RequestState.Requested
    };

    private string MapStateToString(RequestState state) => state switch
    {
        RequestState.Requested => RequestStatus.Pending,
        RequestState.Approved => RequestStatus.Active,
        RequestState.Active => RequestStatus.Active,
        RequestState.Revoked => RequestStatus.Revoked,
        RequestState.Expired => RequestStatus.Expired,
        _ => RequestStatus.Pending
    };
}
