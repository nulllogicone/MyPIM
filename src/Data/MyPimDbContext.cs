using Microsoft.EntityFrameworkCore;

namespace MyPIM.Data;

public class MyPimDbContext : DbContext
{
    public MyPimDbContext(DbContextOptions<MyPimDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Scope> Scopes => Set<Scope>();
    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<GroupRole> GroupRoles => Set<GroupRole>();
    public DbSet<RequestApprover> RequestApprovers => Set<RequestApprover>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.ObjectId).IsUnique();
        });

        modelBuilder.Entity<Group>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.ObjectId).IsUnique();
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.ScopeId);
        });

        modelBuilder.Entity<Scope>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.ArmScope).IsUnique();
        });

        modelBuilder.Entity<Request>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => new { x.Status, x.RequestedByUserId });
            e.HasIndex(x => new { x.Status, x.ExpiresAtUtc });
            e.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId);
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<Approval>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasOne(x => x.Request).WithMany(x => x.Approvals).HasForeignKey(x => x.RequestId);
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
            e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<GroupRole>(e =>
        {
            e.HasKey(x => new { x.GroupId, x.RoleId });
            e.HasOne<Group>().WithMany().HasForeignKey(x => x.GroupId);
            e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<RequestApprover>(e =>
        {
            e.HasKey(x => new { x.RequestId, x.ApproverUserId });
            e.HasOne<Request>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ApproverUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public class User
{
    public Guid Id { get; set; }
    public Guid ObjectId { get; set; } // Entra ID
    public string? DisplayName { get; set; }
}

public class Group
{
    public Guid Id { get; set; }
    public Guid ObjectId { get; set; } // Entra Group ID
    public string? DisplayName { get; set; }
}

public class Scope
{
    public Guid Id { get; set; }
    public string ArmScope { get; set; } = string.Empty; // e.g. "/subscriptions/.../resourceGroups/..."
}

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
    public Scope? Scope { get; set; }
}

public enum RequestState
{
    Requested,
    Approved,
    Active,
    Revoked,
    Expired
}

public class Request
{
    public Guid Id { get; set; }
    public Guid RequestedByUserId { get; set; }
    public User? RequestedByUser { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public RequestState Status { get; set; }
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}

public class Approval
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Request? Request { get; set; }
    public Guid ApprovedByUserId { get; set; }
    public DateTimeOffset ApprovedAtUtc { get; set; }
    public string? Comment { get; set; }
}

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class GroupRole
{
    public Guid GroupId { get; set; }
    public Guid RoleId { get; set; }
}

public class RequestApprover
{
    public Guid RequestId { get; set; }
    public Guid ApproverUserId { get; set; }
}