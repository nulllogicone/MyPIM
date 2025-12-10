using Azure;
using Azure.Data.Tables;

namespace MyPIM.Data;

public class AccessRequest : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // Status (Pending, Active, etc.)
    public string RowKey { get; set; } = Guid.NewGuid().ToString(); // RequestId
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string UserId { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public static class RequestStatus
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Expired = "Expired";
    public const string Rejected = "Rejected";
    public const string Revoked = "Revoked";
}
