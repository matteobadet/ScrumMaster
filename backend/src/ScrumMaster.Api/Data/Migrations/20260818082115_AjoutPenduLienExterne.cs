using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjoutPenduLienExterne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LienExterneNom",
                table: "Etapes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LienExterneUrl",
                table: "Etapes",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotAPendu",
                table: "Etapes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LettresProposeesPendu",
                columns: table => new
                {
                    EtapeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Lettre = table.Column<char>(type: "character(1)", nullable: false),
                    Correcte = table.Column<bool>(type: "boolean", nullable: false),
                    ParticipantProposantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateProposition = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LettresProposeesPendu", x => new { x.EtapeId, x.Lettre });
                    table.ForeignKey(
                        name: "FK_LettresProposeesPendu_Etapes_EtapeId",
                        column: x => x.EtapeId,
                        principalTable: "Etapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LettresProposeesPendu_Participants_ParticipantProposantId",
                        column: x => x.ParticipantProposantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LettresProposeesPendu_ParticipantProposantId",
                table: "LettresProposeesPendu",
                column: "ParticipantProposantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LettresProposeesPendu");

            migrationBuilder.DropColumn(
                name: "LienExterneNom",
                table: "Etapes");

            migrationBuilder.DropColumn(
                name: "LienExterneUrl",
                table: "Etapes");

            migrationBuilder.DropColumn(
                name: "MotAPendu",
                table: "Etapes");
        }
    }
}
