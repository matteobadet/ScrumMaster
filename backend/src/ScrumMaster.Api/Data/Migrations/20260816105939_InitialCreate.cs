using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipes",
                columns: table => new
                {
                    AreaPath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipes", x => x.AreaPath);
                });

            migrationBuilder.CreateTable(
                name: "Themes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    EstPredefini = table.Column<bool>(type: "boolean", nullable: false),
                    EstParDefaut = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaPath = table.Column<string>(type: "text", nullable: false),
                    Iteration = table.Column<string>(type: "text", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MaxVotesParParticipant = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boards_Equipes_AreaPath",
                        column: x => x.AreaPath,
                        principalTable: "Equipes",
                        principalColumn: "AreaPath",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Boards_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Colonnes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Intitule = table.Column<string>(type: "text", nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colonnes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Colonnes_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomAffiche = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participants_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostIts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColonneId = table.Column<Guid>(type: "uuid", nullable: false),
                    Texte = table.Column<string>(type: "text", nullable: false),
                    AuteurParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreation = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostIts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostIts_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostIts_Colonnes_ColonneId",
                        column: x => x.ColonneId,
                        principalTable: "Colonnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostIts_Participants_AuteurParticipantId",
                        column: x => x.AuteurParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    PostItId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => new { x.PostItId, x.ParticipantId });
                    table.ForeignKey(
                        name: "FK_Votes_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Votes_PostIts_PostItId",
                        column: x => x.PostItId,
                        principalTable: "PostIts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Boards_AreaPath",
                table: "Boards",
                column: "AreaPath");

            migrationBuilder.CreateIndex(
                name: "IX_Boards_ThemeId",
                table: "Boards",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Colonnes_ThemeId",
                table: "Colonnes",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_BoardId",
                table: "Participants",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_PostIts_AuteurParticipantId",
                table: "PostIts",
                column: "AuteurParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_PostIts_BoardId",
                table: "PostIts",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_PostIts_ColonneId",
                table: "PostIts",
                column: "ColonneId");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ParticipantId",
                table: "Votes",
                column: "ParticipantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "PostIts");

            migrationBuilder.DropTable(
                name: "Colonnes");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "Boards");

            migrationBuilder.DropTable(
                name: "Equipes");

            migrationBuilder.DropTable(
                name: "Themes");
        }
    }
}
