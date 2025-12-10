using Azure;
using Azure.Data.Tables;

namespace MyPIM.Data;

public class PimRoleConfiguration : ITableEntity
{
    public string PartitionKey { get; set; } = "CONFIG";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string RoleName { get; set; } = string.Empty;
    public int DefaultDurationMinutes { get; set; } = 60;
    public string? ApproverGroupId { get; set; }
    public bool IsEnabled { get; set; } = true;
}
