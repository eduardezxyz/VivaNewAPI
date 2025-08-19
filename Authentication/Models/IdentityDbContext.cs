using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Models;
using NewVivaApi.Authentication;


namespace NewVivaApi.Authentication.Models;

public class Role : IdentityRole<string> // Using string IDs like your existing setup
{
    public ICollection<User>? Users { get; internal set; }
}

public class IdentityDbContext : IdentityDbContext<ApplicationUser, Role, string>
{
    private readonly IConfiguration _configuration;

    public virtual DbSet<AspNetUserExtension> AspNetUserExtensions { get; set; }


    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        IConfiguration configuration
    ) : base(options)
    {
        _configuration = configuration;
    }

    // Add any Identity-related DbSets here
    // Example: public virtual DbSet<EInternalToken> InternalTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // This configures all Identity tables

        // Configure custom Identity-related entities here
        // Example: builder.Entity<EInternalToken>().ToTable("AspNetInternalTokens");

        // Configure ApplicationUser properties
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.TwoFactorType).HasMaxLength(50);
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.JobTitle).HasMaxLength(100);
        });
        
        builder.Entity<AspNetUserExtension>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("AspNetUserExtensions_ClearExpired"));

            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.PasswordResetTokenExpiration).HasColumnType("datetime");

            // entity.HasOne(d => d.IdNavigation).WithOne(p => p.AspNetUserExtension)
            //     .HasForeignKey<AspNetUserExtension>(d => d.Id)
            //     .OnDelete(DeleteBehavior.ClientSetNull)
            //     .HasConstraintName("FK_AspNetUserExtensions_AspNetUsers");
        });
    }
}