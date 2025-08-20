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

    //public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    //public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    //public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    // virtual DbSet<AspNetUserExtension> AspNetUserExtensions { get; set; }

    //public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

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
        
        /*
        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.AspNetRoles");

            entity.HasIndex(e => e.Name, "RoleNameIndex").IsUnique();

            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.AspNetUsers");

            entity.ToTable(tb => tb.HasTrigger("AspNetUsers_FlagResetPassword_I"));

            entity.HasIndex(e => e.UserName, "UserNameIndex").IsUnique();

            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.ResetPasswordOnLoginTf).HasColumnName("ResetPasswordOnLoginTF");
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("PK_dbo.AspNetUserRoles");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_RoleId");
                        j.HasIndex(new[] { "UserId" }, "IX_UserId");
                        j.IndexerProperty<string>("UserId").HasMaxLength(128);
                        j.IndexerProperty<string>("RoleId").HasMaxLength(128);
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.AspNetUserClaims");

            entity.HasIndex(e => e.UserId, "IX_UserId");

            entity.Property(e => e.UserId).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId");
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey, e.UserId }).HasName("PK_dbo.AspNetUserLogins");

            entity.HasIndex(e => e.UserId, "IX_UserId");

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasMaxLength(128);
            entity.Property(e => e.UserId).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId");
        });

        modelBuilder.Entity<AspNetUserExtension>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("AspNetUserExtensions_ClearExpired"));

            entity.Property(e => e.Id).HasMaxLength(128);
            entity.Property(e => e.PasswordResetTokenExpiration).HasColumnType("datetime");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.AspNetUserExtension)
                .HasForeignKey<AspNetUserExtension>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AspNetUserExtensions_AspNetUsers");
        });
        */

    }
}