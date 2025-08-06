using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NewVivaApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "__MigrationHistory",
                columns: table => new
                {
                    MigrationId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContextKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Model = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ProductVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.__MigrationHistory", x => new { x.MigrationId, x.ContextKey });
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEndDateUtc = table.Column<DateTime>(type: "datetime", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ResetPasswordOnLoginTF = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneralContractors",
                columns: table => new
                {
                    GeneralContractorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneralContractorName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VivaGeneralContractorID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusID = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    JsonAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "(getutcdate())"),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LogoImage = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
                    DommainName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedByUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralContractors", x => x.GeneralContractorID);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VivaReportID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreateDT = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdateDT = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DeleteDT = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportID);
                });

            migrationBuilder.CreateTable(
                name: "Subcontractors",
                columns: table => new
                {
                    SubcontractorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubcontractorName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VivaSubcontractorID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StatusID = table.Column<int>(type: "int", nullable: false),
                    JsonAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subcontractors", x => x.SubcontractorID);
                });

            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    AdminUserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.AdminUserID);
                    table.ForeignKey(
                        name: "FK_AdminUsers_AspNetUsers",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserExtensions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PasswordResetIdentity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetTokenExpiration = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserExtensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserExtensions_AspNetUsers",
                        column: x => x.Id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey, x.UserId });
                    table.ForeignKey(
                        name: "FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo.AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceUsers",
                columns: table => new
                {
                    ServiceUserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WebHookURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BearerToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceUsers", x => x.ServiceUserID);
                    table.ForeignKey(
                        name: "FK_ServiceUsers_AspNetUsers",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    UserID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_UserProfile_UserProfile",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GeneralContractorUsers",
                columns: table => new
                {
                    GeneralContractorUserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    GeneralContractorID = table.Column<int>(type: "int", nullable: false),
                    CanApproveTF = table.Column<bool>(type: "bit", nullable: false),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralContractorUsers", x => x.GeneralContractorUserID);
                    table.ForeignKey(
                        name: "FK_GeneralContractorUsers_AspNetUsers",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GeneralContractorUsers_GeneralContractors",
                        column: x => x.GeneralContractorID,
                        principalTable: "GeneralContractors",
                        principalColumn: "GeneralContractorID");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    VivaProjectID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GeneralContractorID = table.Column<int>(type: "int", nullable: false),
                    StatusID = table.Column<int>(type: "int", nullable: false),
                    StartDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    JsonAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDT = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdateDT = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DeleteDT = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedByUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectID);
                    table.ForeignKey(
                        name: "FK_Projects_GeneralContractors",
                        column: x => x.GeneralContractorID,
                        principalTable: "GeneralContractors",
                        principalColumn: "GeneralContractorID");
                });

            migrationBuilder.CreateTable(
                name: "SubcontractorUsers",
                columns: table => new
                {
                    SubcontractorUserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SubcontractorID = table.Column<int>(type: "int", nullable: false),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorUsers", x => x.SubcontractorUserID);
                    table.ForeignKey(
                        name: "FK_SubcontractorUsers_AspNetUsers",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubcontractorUsers_Subcontractors",
                        column: x => x.SubcontractorID,
                        principalTable: "Subcontractors",
                        principalColumn: "SubcontractorID");
                });

            migrationBuilder.CreateTable(
                name: "SubcontractorProjects",
                columns: table => new
                {
                    SubcontractorProjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubcontractorID = table.Column<int>(type: "int", nullable: false),
                    ProjectID = table.Column<int>(type: "int", nullable: false),
                    DiscountPct = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JsonAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusID = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorProjects", x => x.SubcontractorProjectID);
                    table.ForeignKey(
                        name: "FK_SubcontractorProjects_Projects",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID");
                    table.ForeignKey(
                        name: "FK_SubcontractorProjects_Subcontractors",
                        column: x => x.SubcontractorID,
                        principalTable: "Subcontractors",
                        principalColumn: "SubcontractorID");
                });

            migrationBuilder.CreateTable(
                name: "PayApps",
                columns: table => new
                {
                    PayAppID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VivaPayAppID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubcontractorProjectID = table.Column<int>(type: "int", nullable: false),
                    StatusID = table.Column<int>(type: "int", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    JsonAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoryAttributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(getutcdate())"),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayApps", x => x.PayAppID);
                    table.ForeignKey(
                        name: "FK_PayApps_SubcontractorProjects",
                        column: x => x.SubcontractorProjectID,
                        principalTable: "SubcontractorProjects",
                        principalColumn: "SubcontractorProjectID");
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentTypeID = table.Column<int>(type: "int", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DownloadFileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreateByUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PayAppID = table.Column<int>(type: "int", nullable: true),
                    SubcontractorID = table.Column<int>(type: "int", nullable: true),
                    SubcontractorProjectID = table.Column<int>(type: "int", nullable: true),
                    GeneralContractorID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentID);
                    table.ForeignKey(
                        name: "FK_Documents_GeneralContractors",
                        column: x => x.GeneralContractorID,
                        principalTable: "GeneralContractors",
                        principalColumn: "GeneralContractorID");
                    table.ForeignKey(
                        name: "FK_Documents_PayApps",
                        column: x => x.PayAppID,
                        principalTable: "PayApps",
                        principalColumn: "PayAppID");
                    table.ForeignKey(
                        name: "FK_Documents_SubcontractorProjects",
                        column: x => x.SubcontractorProjectID,
                        principalTable: "SubcontractorProjects",
                        principalColumn: "SubcontractorProjectID");
                    table.ForeignKey(
                        name: "FK_Documents_Subcontractors",
                        column: x => x.SubcontractorID,
                        principalTable: "Subcontractors",
                        principalColumn: "SubcontractorID");
                });

            migrationBuilder.CreateTable(
                name: "PayAppHistory",
                columns: table => new
                {
                    PayAppHistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayAppID = table.Column<int>(type: "int", nullable: false),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Event = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LowestPermToView = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayAppHistory_1", x => x.PayAppHistoryID);
                    table.ForeignKey(
                        name: "FK_PayAppHistory_PayApps",
                        column: x => x.PayAppID,
                        principalTable: "PayApps",
                        principalColumn: "PayAppID");
                });

            migrationBuilder.CreateTable(
                name: "PayAppPayments",
                columns: table => new
                {
                    PaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayAppID = table.Column<int>(type: "int", nullable: false),
                    PaymentTypeID = table.Column<int>(type: "int", nullable: false),
                    SubcontractorID = table.Column<int>(type: "int", nullable: true),
                    DollarAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JsonAttributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DeleteDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayAppPayments", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_PayAppPayments_PayApps",
                        column: x => x.PayAppID,
                        principalTable: "PayApps",
                        principalColumn: "PayAppID");
                    table.ForeignKey(
                        name: "FK_PayAppPayments_Subcontractors",
                        column: x => x.SubcontractorID,
                        principalTable: "Subcontractors",
                        principalColumn: "SubcontractorID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_UserID",
                table: "AdminUsers",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserId",
                table: "AspNetUserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_GeneralContractorID",
                table: "Documents",
                column: "GeneralContractorID");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PayAppID",
                table: "Documents",
                column: "PayAppID");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SubcontractorID",
                table: "Documents",
                column: "SubcontractorID");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SubcontractorProjectID",
                table: "Documents",
                column: "SubcontractorProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralContractorUsers_GeneralContractorID",
                table: "GeneralContractorUsers",
                column: "GeneralContractorID");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralContractorUsers_UserID",
                table: "GeneralContractorUsers",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_PayAppHistory_PayAppID",
                table: "PayAppHistory",
                column: "PayAppID");

            migrationBuilder.CreateIndex(
                name: "IX_PayAppPayments_PayAppID",
                table: "PayAppPayments",
                column: "PayAppID");

            migrationBuilder.CreateIndex(
                name: "IX_PayAppPayments_SubcontractorID",
                table: "PayAppPayments",
                column: "SubcontractorID");

            migrationBuilder.CreateIndex(
                name: "IX_PayApps_SubcontractorProjectID",
                table: "PayApps",
                column: "SubcontractorProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_GeneralContractorID",
                table: "Projects",
                column: "GeneralContractorID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUsers_UserID",
                table: "ServiceUsers",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorProjects_ProjectID",
                table: "SubcontractorProjects",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorProjects_SubcontractorID",
                table: "SubcontractorProjects",
                column: "SubcontractorID");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorUsers_SubcontractorID",
                table: "SubcontractorUsers",
                column: "SubcontractorID");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorUsers_UserID",
                table: "SubcontractorUsers",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__MigrationHistory");

            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserExtensions");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "GeneralContractorUsers");

            migrationBuilder.DropTable(
                name: "PayAppHistory");

            migrationBuilder.DropTable(
                name: "PayAppPayments");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "ServiceUsers");

            migrationBuilder.DropTable(
                name: "SubcontractorUsers");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "PayApps");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "SubcontractorProjects");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Subcontractors");

            migrationBuilder.DropTable(
                name: "GeneralContractors");
        }
    }
}
