using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRappelEnvoye : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RappelsEnvoyes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaPath = table.Column<string>(type: "text", nullable: false),
                    TypeReunion = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DateEnvoi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RappelsEnvoyes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RappelsEnvoyes_Equipes_AreaPath",
                        column: x => x.AreaPath,
                        principalTable: "Equipes",
                        principalColumn: "AreaPath",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RappelsEnvoyes_AreaPath_TypeReunion_Date",
                table: "RappelsEnvoyes",
                columns: new[] { "AreaPath", "TypeReunion", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RappelsEnvoyes");
        }
    }
}
