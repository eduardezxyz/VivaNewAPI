using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Models;

namespace NewVivaApi.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminUser> AdminUsers { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserExtension> AspNetUserExtensions { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentsVw> DocumentsVws { get; set; }

    public virtual DbSet<GeneralContractor> GeneralContractors { get; set; }

    public virtual DbSet<GeneralContractorUser> GeneralContractorUsers { get; set; }

    public virtual DbSet<GeneralContractorsVw> GeneralContractorsVws { get; set; }

    public virtual DbSet<MigrationHistory> MigrationHistories { get; set; }

    public virtual DbSet<PayApp> PayApps { get; set; }

    public virtual DbSet<PayAppHistory> PayAppHistories { get; set; }

    public virtual DbSet<PayAppHistoryVw> PayAppHistoryVws { get; set; }

    public virtual DbSet<PayAppPayment> PayAppPayments { get; set; }

    public virtual DbSet<PayAppPaymentsVw> PayAppPaymentsVws { get; set; }

    public virtual DbSet<PayAppsUnMaskedVw> PayAppsUnMaskedVws { get; set; }

    public virtual DbSet<PayAppsVw> PayAppsVws { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectsVw> ProjectsVws { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ReportsVw> ReportsVws { get; set; }

    public virtual DbSet<ServiceUser> ServiceUsers { get; set; }

    public virtual DbSet<Subcontractor> Subcontractors { get; set; }

    public virtual DbSet<SubcontractorProject> SubcontractorProjects { get; set; }

    public virtual DbSet<SubcontractorProjectsVw> SubcontractorProjectsVws { get; set; }

    public virtual DbSet<SubcontractorUser> SubcontractorUsers { get; set; }

    public virtual DbSet<SubcontractorsVw> SubcontractorsVws { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserProfilesVw> UserProfilesVws { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=54.190.6.1;Database=VivaPayApp_DEV;User Id=esalugsugan;Password=x2EAtIuYmx8JIBs4;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.Property(e => e.AdminUserId).HasColumnName("AdminUserID");
            entity.Property(e => e.CreateDt).HasColumnName("CreateDT");
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.LastUpdateDt).HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.UserId)
                .HasMaxLength(128)
                .HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.AdminUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdminUsers_AspNetUsers");
        });

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

        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(e => e.DocumentId).HasColumnName("DocumentID");
            entity.Property(e => e.Bucket).HasMaxLength(250);
            entity.Property(e => e.CreateByUser).HasMaxLength(250);
            entity.Property(e => e.CreateDt).HasColumnName("CreateDT");
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.DocumentTypeId).HasColumnName("DocumentTypeID");
            entity.Property(e => e.DownloadFileName).HasMaxLength(250);
            entity.Property(e => e.FileName).HasMaxLength(250);
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.Path).HasMaxLength(250);
            entity.Property(e => e.PayAppId).HasColumnName("PayAppID");
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");
            entity.Property(e => e.SubcontractorProjectId).HasColumnName("SubcontractorProjectID");

            entity.HasOne(d => d.GeneralContractor).WithMany(p => p.Documents)
                .HasForeignKey(d => d.GeneralContractorId)
                .HasConstraintName("FK_Documents_GeneralContractors");

            entity.HasOne(d => d.PayApp).WithMany(p => p.Documents)
                .HasForeignKey(d => d.PayAppId)
                .HasConstraintName("FK_Documents_PayApps");

            entity.HasOne(d => d.Subcontractor).WithMany(p => p.Documents)
                .HasForeignKey(d => d.SubcontractorId)
                .HasConstraintName("FK_Documents_Subcontractors");

            entity.HasOne(d => d.SubcontractorProject).WithMany(p => p.Documents)
                .HasForeignKey(d => d.SubcontractorProjectId)
                .HasConstraintName("FK_Documents_SubcontractorProjects");
        });

        modelBuilder.Entity<DocumentsVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Documents_vw");

            entity.Property(e => e.Bucket).HasMaxLength(250);
            entity.Property(e => e.CreateByUser).HasMaxLength(250);
            entity.Property(e => e.CreateDt).HasColumnName("CreateDT");
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.DocumentId)
                .ValueGeneratedOnAdd()
                .HasColumnName("DocumentID");
            entity.Property(e => e.DocumentTypeId).HasColumnName("DocumentTypeID");
            entity.Property(e => e.DownloadFileName).HasMaxLength(250);
            entity.Property(e => e.FileName).HasMaxLength(250);
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.Path).HasMaxLength(250);
            entity.Property(e => e.PayAppId).HasColumnName("PayAppID");
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");
            entity.Property(e => e.SubcontractorProjectId).HasColumnName("SubcontractorProjectID");
        });

        modelBuilder.Entity<GeneralContractor>(entity =>
        {
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.CreateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("CreateDT");
            entity.Property(e => e.CreatedByUser).HasMaxLength(100);
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.DommainName).HasMaxLength(250);
            entity.Property(e => e.GeneralContractorName).HasMaxLength(250);
            entity.Property(e => e.LastUpdateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.LogoImage).IsUnicode(false);
            entity.Property(e => e.StatusId)
                .HasDefaultValue(1)
                .HasColumnName("StatusID");
            entity.Property(e => e.VivaGeneralContractorId)
                .HasMaxLength(50)
                .HasColumnName("VivaGeneralContractorID");
        });

        modelBuilder.Entity<GeneralContractorUser>(entity =>
        {
            entity.Property(e => e.GeneralContractorUserId).HasColumnName("GeneralContractorUserID");
            entity.Property(e => e.CanApproveTf).HasColumnName("CanApproveTF");
            entity.Property(e => e.CreateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("CreateDT");
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.LastUpdateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.UserId)
                .HasMaxLength(128)
                .HasColumnName("UserID");

            entity.HasOne(d => d.GeneralContractor).WithMany(p => p.GeneralContractorUsers)
                .HasForeignKey(d => d.GeneralContractorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GeneralContractorUsers_GeneralContractors");

            entity.HasOne(d => d.User).WithMany(p => p.GeneralContractorUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GeneralContractorUsers_AspNetUsers");
        });

        modelBuilder.Entity<GeneralContractorsVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("GeneralContractors_vw");

            entity.Property(e => e.CreatedByUser).HasMaxLength(100);
            entity.Property(e => e.DommainName).HasMaxLength(250);
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.GeneralContractorName).HasMaxLength(250);
            entity.Property(e => e.LogoImage).IsUnicode(false);
            entity.Property(e => e.NumSubs).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.Outstanding).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.VivaGeneralContractorId)
                .HasMaxLength(50)
                .HasColumnName("VivaGeneralContractorID");
        });

        modelBuilder.Entity<MigrationHistory>(entity =>
        {
            entity.HasKey(e => new { e.MigrationId, e.ContextKey }).HasName("PK_dbo.__MigrationHistory");

            entity.ToTable("__MigrationHistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ContextKey).HasMaxLength(300);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<PayApp>(entity =>
        {
            entity.ToTable(tb =>
                {
                    tb.HasTrigger("AfterInsertAddHistoryRecord_Trigger");
                    tb.HasTrigger("AfterUpdateAddHistoryRecord_Trigger");
                });

            entity.Property(e => e.PayAppId).HasColumnName("PayAppID");
            entity.Property(e => e.ApprovalDt).HasColumnName("ApprovalDT");
            entity.Property(e => e.ApprovedAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("CreateDT");
            entity.Property(e => e.CreatedByUser).HasMaxLength(100);
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.LastUpdateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.RequestedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.SubcontractorProjectId).HasColumnName("SubcontractorProjectID");
            entity.Property(e => e.VivaPayAppId)
                .HasMaxLength(50)
                .HasColumnName("VivaPayAppID");

            entity.HasOne(d => d.SubcontractorProject).WithMany(p => p.PayApps)
                .HasForeignKey(d => d.SubcontractorProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayApps_SubcontractorProjects");
        });

        modelBuilder.Entity<PayAppHistory>(entity =>
        {
            entity.HasKey(e => e.PayAppHistoryId).HasName("PK_PayAppHistory_1");

            entity.ToTable("PayAppHistory");

            entity.Property(e => e.PayAppHistoryId).HasColumnName("PayAppHistoryID");
            entity.Property(e => e.CreateDt).HasColumnName("CreateDT");
            entity.Property(e => e.LastUpdateDt).HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.LowestPermToView).HasMaxLength(15);
            entity.Property(e => e.PayAppId).HasColumnName("PayAppID");

            entity.HasOne(d => d.PayApp).WithMany(p => p.PayAppHistories)
                .HasForeignKey(d => d.PayAppId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayAppHistory_PayApps");
        });

        modelBuilder.Entity<PayAppHistoryVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PayAppHistory_vw");

            entity.Property(e => e.CreateDt).HasColumnName("CreateDT");
            entity.Property(e => e.LastUpdateDt).HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.LowestPermToView).HasMaxLength(15);
            entity.Property(e => e.PayAppHistoryId)
                .ValueGeneratedOnAdd()
                .HasColumnName("PayAppHistoryID");
            entity.Property(e => e.PayAppId).HasColumnName("PayAppID");
        });

        modelBuilder.Entity<PayAppPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.CreateDt).HasColumnName("CreateDT");
            entity.Property(e => e.CreatedByUser).HasMaxLength(100);
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.DollarAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LastUpdateDt).HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.PayAppId).HasColumnName("PayAppID");
            entity.Property(e => e.PaymentTypeId).HasColumnName("PaymentTypeID");
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");

            entity.HasOne(d => d.PayApp).WithMany(p => p.PayAppPayments)
                .HasForeignKey(d => d.PayAppId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayAppPayments_PayApps");

            entity.HasOne(d => d.Subcontractor).WithMany(p => p.PayAppPayments)
                .HasForeignKey(d => d.SubcontractorId)
                .HasConstraintName("FK_PayAppPayments_Subcontractors");
        });

        modelBuilder.Entity<PayAppPaymentsVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PayAppPayments_vw");

            entity.Property(e => e.CreateDt).HasColumnName("CreateDT");
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.DollarAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LastUpdateDt).HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.PayAppId).HasColumnName("PayAppID");
            entity.Property(e => e.PaymentId)
                .ValueGeneratedOnAdd()
                .HasColumnName("PaymentID");
            entity.Property(e => e.PaymentType)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.PaymentTypeId).HasColumnName("PaymentTypeID");
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");
        });

        modelBuilder.Entity<PayAppsUnMaskedVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PayApps_unMasked_vw");

            entity.Property(e => e.ApprovalDt).HasColumnName("ApprovalDT");
            entity.Property(e => e.ApprovedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.PayAppId).HasColumnName("PayAppID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.RequestedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");
            entity.Property(e => e.SubcontractorProjectId).HasColumnName("SubcontractorProjectID");
            entity.Property(e => e.VivaPayAppId)
                .HasMaxLength(50)
                .HasColumnName("VivaPayAppID");
        });

        modelBuilder.Entity<PayAppsVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("PayApps_vw");

            entity.Property(e => e.ApprovalDt).HasColumnName("ApprovalDT");
            entity.Property(e => e.ApprovedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedByUser).HasMaxLength(100);
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.PayAppId).HasColumnName("PayAppID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.RequestedAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");
            entity.Property(e => e.SubcontractorProjectId).HasColumnName("SubcontractorProjectID");
            entity.Property(e => e.VivaPayAppId)
                .HasMaxLength(50)
                .HasColumnName("VivaPayAppID");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.CreateDt)
                .HasColumnType("datetime")
                .HasColumnName("CreateDT");
            entity.Property(e => e.CreatedByUser).HasMaxLength(100);
            entity.Property(e => e.DeleteDt)
                .HasColumnType("datetime")
                .HasColumnName("DeleteDT");
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.LastUpdateDt)
                .HasColumnType("datetime")
                .HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.ProjectName).HasMaxLength(250);
            entity.Property(e => e.StartDt).HasColumnName("StartDT");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.VivaProjectId)
                .HasMaxLength(50)
                .HasColumnName("VivaProjectID");

            entity.HasOne(d => d.GeneralContractor).WithMany(p => p.Projects)
                .HasForeignKey(d => d.GeneralContractorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Projects_GeneralContractors");
        });

        modelBuilder.Entity<ProjectsVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Projects_vw");

            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.ProjectName).HasMaxLength(250);
            entity.Property(e => e.StartDt).HasColumnName("StartDT");
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.UnpaidBalance).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.VivaProjectId)
                .HasMaxLength(50)
                .HasColumnName("VivaProjectID");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.CreateDt)
                .HasColumnType("datetime")
                .HasColumnName("CreateDT");
            entity.Property(e => e.DeleteDt)
                .HasColumnType("datetime")
                .HasColumnName("DeleteDT");
            entity.Property(e => e.LastUpdateDt)
                .HasColumnType("datetime")
                .HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.ReportName).HasMaxLength(250);
            entity.Property(e => e.VivaReportId)
                .HasMaxLength(50)
                .HasColumnName("VivaReportID");
        });

        modelBuilder.Entity<ReportsVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Reports_vw");

            entity.Property(e => e.CreateDt)
                .HasColumnType("datetime")
                .HasColumnName("CreateDT");
            entity.Property(e => e.ReportId).HasColumnName("ReportID");
            entity.Property(e => e.ReportName).HasMaxLength(250);
            entity.Property(e => e.VivaReportId)
                .HasMaxLength(50)
                .HasColumnName("VivaReportID");
        });

        modelBuilder.Entity<ServiceUser>(entity =>
        {
            entity.Property(e => e.ServiceUserId).HasColumnName("ServiceUserID");
            entity.Property(e => e.BearerToken).HasMaxLength(500);
            entity.Property(e => e.CreateDt).HasColumnName("CreateDT");
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.LastUpdateDt).HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.UserId)
                .HasMaxLength(128)
                .HasColumnName("UserID");
            entity.Property(e => e.WebHookUrl)
                .HasMaxLength(500)
                .HasColumnName("WebHookURL");

            entity.HasOne(d => d.User).WithMany(p => p.ServiceUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceUsers_AspNetUsers");
        });

        modelBuilder.Entity<Subcontractor>(entity =>
        {
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");
            entity.Property(e => e.CreateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("CreateDT");
            entity.Property(e => e.CreatedByUser).HasMaxLength(100);
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.LastUpdateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.SubcontractorName).HasMaxLength(250);
            entity.Property(e => e.VivaSubcontractorId)
                .HasMaxLength(50)
                .HasColumnName("VivaSubcontractorID");
        });

        modelBuilder.Entity<SubcontractorProject>(entity =>
        {
            entity.Property(e => e.SubcontractorProjectId).HasColumnName("SubcontractorProjectID");
            entity.Property(e => e.CreateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("CreateDT");
            entity.Property(e => e.CreatedByUser).HasMaxLength(100);
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.DiscountPct).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LastUpdateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.StatusId)
                .HasDefaultValue(1)
                .HasColumnName("StatusID");
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");

            entity.HasOne(d => d.Project).WithMany(p => p.SubcontractorProjects)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubcontractorProjects_Projects");

            entity.HasOne(d => d.Subcontractor).WithMany(p => p.SubcontractorProjects)
                .HasForeignKey(d => d.SubcontractorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubcontractorProjects_Subcontractors");
        });

        modelBuilder.Entity<SubcontractorProjectsVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("SubcontractorProjects_vw");

            entity.Property(e => e.Contact).HasMaxLength(4000);
            entity.Property(e => e.ContactEmail).HasMaxLength(4000);
            entity.Property(e => e.DiscountPct).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.ProjectId).HasColumnName("ProjectID");
            entity.Property(e => e.ProjectName).HasMaxLength(250);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");
            entity.Property(e => e.SubcontractorName).HasMaxLength(250);
            entity.Property(e => e.SubcontractorProjectId).HasColumnName("SubcontractorProjectID");
        });

        modelBuilder.Entity<SubcontractorUser>(entity =>
        {
            entity.Property(e => e.SubcontractorUserId).HasColumnName("SubcontractorUserID");
            entity.Property(e => e.CreateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("CreateDT");
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.LastUpdateDt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");
            entity.Property(e => e.UserId)
                .HasMaxLength(128)
                .HasColumnName("UserID");

            entity.HasOne(d => d.Subcontractor).WithMany(p => p.SubcontractorUsers)
                .HasForeignKey(d => d.SubcontractorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubcontractorUsers_Subcontractors");

            entity.HasOne(d => d.User).WithMany(p => p.SubcontractorUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubcontractorUsers_AspNetUsers");
        });

        modelBuilder.Entity<SubcontractorsVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("Subcontractors_vw");

            entity.Property(e => e.CreatedByUser).HasMaxLength(100);
            entity.Property(e => e.StatusId).HasColumnName("StatusID");
            entity.Property(e => e.SubcontractorId)
                .ValueGeneratedOnAdd()
                .HasColumnName("SubcontractorID");
            entity.Property(e => e.SubcontractorName).HasMaxLength(250);
            entity.Property(e => e.VivaSubcontractorId)
                .HasMaxLength(50)
                .HasColumnName("VivaSubcontractorID");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_UserProfile");

            entity.Property(e => e.UserId)
                .HasMaxLength(128)
                .HasColumnName("UserID");
            entity.Property(e => e.CreateDt).HasColumnName("CreateDT");
            entity.Property(e => e.DeleteDt).HasColumnName("DeleteDT");
            entity.Property(e => e.FirstName).HasMaxLength(250);
            entity.Property(e => e.LastName).HasMaxLength(250);
            entity.Property(e => e.LastUpdateDt).HasColumnName("LastUpdateDT");
            entity.Property(e => e.LastUpdateUser).HasMaxLength(250);
            entity.Property(e => e.PhoneNumber).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserProfile_UserProfile");
        });

        modelBuilder.Entity<UserProfilesVw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("UserProfiles_vw");

            entity.Property(e => e.CompanyName).HasMaxLength(250);
            entity.Property(e => e.FirstName).HasMaxLength(250);
            entity.Property(e => e.FullName).HasMaxLength(501);
            entity.Property(e => e.GeneralContractorId).HasColumnName("GeneralContractorID");
            entity.Property(e => e.LastName).HasMaxLength(250);
            entity.Property(e => e.Password)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber).HasMaxLength(100);
            entity.Property(e => e.SubcontractorId).HasColumnName("SubcontractorID");
            entity.Property(e => e.UserId)
                .HasMaxLength(128)
                .HasColumnName("UserID");
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.UserStatus)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.UserType)
                .HasMaxLength(18)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
