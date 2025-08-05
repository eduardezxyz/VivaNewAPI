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
                name: "Projects",
                columns: table => new
                {
                    ProjectID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VivaProjectID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneralContractorID = table.Column<int>(type: "int", nullable: false),
                    StatusID = table.Column<int>(type: "int", nullable: false),
                    StartDT = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    JsonAttributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreateDT = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateDT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdateUser = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteDT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUser = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
