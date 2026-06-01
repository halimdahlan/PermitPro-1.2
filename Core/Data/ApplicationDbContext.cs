using PermitPro.Core.Entities;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace PermitPro.Core.Data;


public class UserRole : IdentityUserRole<string>
{
    public virtual UserInfo? User { get; set; }
    public virtual Role? Role { get; set; }
}


public class Role : IdentityRole
{
    public virtual List<UserRole>? UserRoles { get; set; }
    public virtual List<SystemMenu>? SystemMenus { get; set; }
}


/// <summary>
/// Application database context used for the app
/// </summary>
public class ApplicationDbContext
    : IdentityDbContext<UserInfo, Role, string, IdentityUserClaim<string>, UserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>
{
    /// <summary>
    /// The default constructor
    /// </summary>
    /// <param name="options">Database context options</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; }

    public DbSet<Site> Sites { get; set; }

    public DbSet<Division> Divisions { get; set; }

    public DbSet<Department> Departments { get; set; }

    public DbSet<Address> Addresses { get; set; }

    public DbSet<Contact> Contacts { get; set; }

    public DbSet<Permit> Permits { get; set; }

    //public DbSet<SitePermit> SitesPermits { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }

    public DbSet<Workflow> Workflows { get; set; }

    public DbSet<WorkflowStep> WorkflowSteps { get; set; }

    public DbSet<WorkflowHistory> WorkflowHistories { get; set; }

    public DbSet<Attachment> Attachments { get; set; }

    public DbSet<Certificate> Certificates { get; set; }

    public DbSet<Notification> Notifications { get; set; }

    public DbSet<PermitNumber> PermitNumbers { get; set; }

    public DbSet<SystemMenu> SystemMenus { get; set; }


    /// <summary>
    /// An override on model creating
    /// </summary>
    /// <param name="builder">Model builder</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserInfo>(options =>
        {
            options.HasMany(x => x.UserRoles)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .IsRequired();

            options.HasMany(x => x.Sites)
                .WithMany(x => x.Users);

            options.HasOne(x => x.UserCompany);

            options.HasMany(x => x.UserNotifications)
                .WithOne(x => x.NotificationUser)
                .OnDelete(DeleteBehavior.Cascade);

            options.HasMany(x => x.WorkflowSteps)
                .WithMany(x => x.Approvers)
                .UsingEntity(x => x.ToTable("ApproversWorkflowSteps"));

            options.HasMany(x => x.AuditLogs)
                .WithOne(x => x.AuditLogUser)
                .OnDelete(DeleteBehavior.Cascade);

        });

        builder.Entity<Role>(options =>
        {
            options.HasMany(x => x.UserRoles)
                .WithOne(x => x.Role)
                .HasForeignKey(x => x.RoleId)
                .IsRequired();
        });

        builder.Entity<Site>(options =>
        {
            options.HasMany(x => x.Users)
                .WithMany(x => x.Sites);
        });

        builder.Entity<Site>()
            .HasMany(x => x.Permits)
            .WithOne(x => x.Site)
            .HasForeignKey("SiteId")
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Company>(options =>
        {
            options.HasMany(x => x.CompanyAddresses)
                .WithOne(x => x.AddressCompany)
                .OnDelete(DeleteBehavior.Cascade);

            options.HasMany(x => x.CompanySites)
                .WithOne(x => x.SiteCompany)
                .OnDelete(DeleteBehavior.Cascade);

            options.HasMany(x => x.CompanyWorkflows)
                .WithOne(x => x.WorkflowCompany)
                .OnDelete(DeleteBehavior.Cascade);

            options.HasMany(x => x.CompanyUsers)
                .WithOne(x => x.UserCompany);

            options.HasMany(x => x.CompanyPermits)
                .WithOne(x => x.Company);

        });

        builder.Entity<Permit>(options =>
        {
            options.HasMany(x => x.Attachments)
                .WithOne(x => x.Permit)
                .OnDelete(DeleteBehavior.Cascade);

            options.HasMany(x => x.WorkflowHistories)
                .WithOne(x => x.Permit)
                .OnDelete(DeleteBehavior.Cascade);

            options.HasMany(x => x.Certificates)
                .WithMany(x => x.Permits)
                .UsingEntity(x => x.ToTable("PermitsCertificates"));
        });

        builder.Entity<Workflow>(options =>
        {
            options.HasMany(x => x.WorkflowSteps)
                .WithOne(x => x.WorkflowStepWorkflow)
                .OnDelete(DeleteBehavior.Cascade);

            options.HasMany(x => x.WorkflowHistories)
                .WithOne(x => x.HistoryWorkflow)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SystemMenu>(options =>
        {
            options.HasMany(x => x.Roles)
                .WithMany(x => x.SystemMenus)
                .UsingEntity(x => x.ToTable("SystemMenusRoles"));
        });

        builder.Entity<Address>();

        builder.Entity<Contact>();

    }
}