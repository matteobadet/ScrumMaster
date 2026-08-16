using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScrumMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAzureDevOpsIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkItemExporteId",
                table: "PostIts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkItemSourceId",
                table: "PostIts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfigurationsAzureDevOps",
                columns: table => new
                {
                    AreaPath = table.Column<string>(type: "text", nullable: false),
                    Organisation = table.Column<string>(type: "text", nullable: false),
                    Projet = table.Column<string>(type: "text", nullable: false),
                    PatChiffre = table.Column<string>(type: "text", nullable: false),
                    DateConfiguration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationsAzureDevOps", x => x.AreaPath);
                    table.ForeignKey(
                        name: "FK_ConfigurationsAzureDevOps_Equipes_AreaPath",
                        column: x => x.AreaPath,
                        principalTable: "Equipes",
                        principalColumn: "AreaPath",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationsAzureDevOps");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropColumn(
                name: "WorkItemExporteId",
                table: "PostIts");

            migrationBuilder.DropColumn(
                name: "WorkItemSourceId",
                table: "PostIts");
        }
    }
}
