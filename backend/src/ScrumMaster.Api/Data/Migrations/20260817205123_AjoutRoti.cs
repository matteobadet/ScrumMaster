using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjoutRoti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReponsesRoti",
                columns: table => new
                {
                    EtapeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Niveau = table.Column<int>(type: "integer", nullable: false),
                    DateReponse = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReponsesRoti", x => new { x.EtapeId, x.ParticipantId });
                    table.ForeignKey(
                        name: "FK_ReponsesRoti_Etapes_EtapeId",
                        column: x => x.EtapeId,
                        principalTable: "Etapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReponsesRoti_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisuelsRoti",
                columns: table => new
                {
                    EtapeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Niveau = table.Column<int>(type: "integer", nullable: false),
                    UrlIllustration = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisuelsRoti", x => new { x.EtapeId, x.Niveau });
                    table.ForeignKey(
                        name: "FK_VisuelsRoti_Etapes_EtapeId",
                        column: x => x.EtapeId,
                        principalTable: "Etapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReponsesRoti_ParticipantId",
                table: "ReponsesRoti",
                column: "ParticipantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReponsesRoti");

            migrationBuilder.DropTable(
                name: "VisuelsRoti");
        }
    }
}
