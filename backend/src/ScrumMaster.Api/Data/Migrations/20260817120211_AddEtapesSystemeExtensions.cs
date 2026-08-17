using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEtapesSystemeExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ordre volontairement différent de celui scaffoldé par défaut : les nouvelles tables
            // doivent exister et être peuplées par backfill (research.md#3, data-model.md) AVANT
            // que Boards.ThemeId ne soit supprimée et que PostIts.BoardId ne soit renommée en
            // EtapeId — sans quoi l'ancienne association Board→Thème et PostIt→Board serait
            // perdue avant d'avoir pu être reportée sur les nouvelles Étapes.
            migrationBuilder.DropForeignKey(
                name: "FK_PostIts_Boards_BoardId",
                table: "PostIts");

            migrationBuilder.CreateTable(
                name: "MiniJeuxCatalogue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    TypeInterne = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiniJeuxCatalogue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Etapes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    ThemeId = table.Column<Guid>(type: "uuid", nullable: true),
                    MiniJeuCatalogueId = table.Column<Guid>(type: "uuid", nullable: true),
                    Question = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etapes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Etapes_Boards_BoardId",
                        column: x => x.BoardId,
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Etapes_MiniJeuxCatalogue_MiniJeuCatalogueId",
                        column: x => x.MiniJeuCatalogueId,
                        principalTable: "MiniJeuxCatalogue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Etapes_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OptionsPollPersonnalise",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EtapeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Texte = table.Column<string>(type: "text", nullable: false),
                    Ordre = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionsPollPersonnalise", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionsPollPersonnalise_Etapes_EtapeId",
                        column: x => x.EtapeId,
                        principalTable: "Etapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReponsesMeteoEquipe",
                columns: table => new
                {
                    EtapeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Humeur = table.Column<int>(type: "integer", nullable: false),
                    DateReponse = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReponsesMeteoEquipe", x => new { x.EtapeId, x.ParticipantId });
                    table.ForeignKey(
                        name: "FK_ReponsesMeteoEquipe_Etapes_EtapeId",
                        column: x => x.EtapeId,
                        principalTable: "Etapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReponsesMeteoEquipe_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReponsesPollPersonnalise",
                columns: table => new
                {
                    EtapeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateReponse = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReponsesPollPersonnalise", x => new { x.EtapeId, x.ParticipantId });
                    table.ForeignKey(
                        name: "FK_ReponsesPollPersonnalise_Etapes_EtapeId",
                        column: x => x.EtapeId,
                        principalTable: "Etapes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReponsesPollPersonnalise_OptionsPollPersonnalise_OptionId",
                        column: x => x.OptionId,
                        principalTable: "OptionsPollPersonnalise",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReponsesPollPersonnalise_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Etapes_BoardId_Ordre",
                table: "Etapes",
                columns: new[] { "BoardId", "Ordre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Etapes_MiniJeuCatalogueId",
                table: "Etapes",
                column: "MiniJeuCatalogueId");

            migrationBuilder.CreateIndex(
                name: "IX_Etapes_ThemeId",
                table: "Etapes",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_MiniJeuxCatalogue_TypeInterne",
                table: "MiniJeuxCatalogue",
                column: "TypeInterne",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OptionsPollPersonnalise_EtapeId",
                table: "OptionsPollPersonnalise",
                column: "EtapeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReponsesMeteoEquipe_ParticipantId",
                table: "ReponsesMeteoEquipe",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReponsesPollPersonnalise_OptionId",
                table: "ReponsesPollPersonnalise",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReponsesPollPersonnalise_ParticipantId",
                table: "ReponsesPollPersonnalise",
                column: "ParticipantId");

            // Backfill (research.md#3, data-model.md) : une Étape "Colonnes et post-its" par Board
            // existant, reprenant son Thème et son Statut, AVANT que Boards.ThemeId ne soit
            // supprimée. TypeEtape.ColonnesEtPostIts = 0. StatutEtape.Active = 1 / Terminee = 2
            // (reprend BoardStatut.Actif = 0 / Cloture = 1).
            migrationBuilder.Sql(
                """
                INSERT INTO "Etapes" ("Id", "BoardId", "Type", "Ordre", "Statut", "ThemeId")
                SELECT (md5(random()::text || clock_timestamp()::text))::uuid, "Id", 0, 0,
                    CASE WHEN "Statut" = 0 THEN 1 ELSE 2 END, "ThemeId"
                FROM "Boards";
                """
            );

            // Repointe les post-its existants vers l'Étape nouvellement créée pour leur Board,
            // pendant que la colonne s'appelle encore "BoardId" (avant le RenameColumn ci-dessous).
            migrationBuilder.Sql(
                """
                UPDATE "PostIts" p
                SET "BoardId" = e."Id"
                FROM "Etapes" e
                WHERE e."BoardId" = p."BoardId";
                """
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Boards_Themes_ThemeId",
                table: "Boards");

            migrationBuilder.DropIndex(
                name: "IX_Boards_ThemeId",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "ThemeId",
                table: "Boards");

            migrationBuilder.RenameColumn(
                name: "BoardId",
                table: "PostIts",
                newName: "EtapeId");

            migrationBuilder.RenameIndex(
                name: "IX_PostIts_BoardId",
                table: "PostIts",
                newName: "IX_PostIts_EtapeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostIts_Etapes_EtapeId",
                table: "PostIts",
                column: "EtapeId",
                principalTable: "Etapes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostIts_Etapes_EtapeId",
                table: "PostIts");

            migrationBuilder.DropTable(
                name: "ReponsesMeteoEquipe");

            migrationBuilder.DropTable(
                name: "ReponsesPollPersonnalise");

            migrationBuilder.DropTable(
                name: "OptionsPollPersonnalise");

            migrationBuilder.DropTable(
                name: "Etapes");

            migrationBuilder.DropTable(
                name: "MiniJeuxCatalogue");

            migrationBuilder.RenameColumn(
                name: "EtapeId",
                table: "PostIts",
                newName: "BoardId");

            migrationBuilder.RenameIndex(
                name: "IX_PostIts_EtapeId",
                table: "PostIts",
                newName: "IX_PostIts_BoardId");

            migrationBuilder.AddColumn<Guid>(
                name: "ThemeId",
                table: "Boards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Boards_ThemeId",
                table: "Boards",
                column: "ThemeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boards_Themes_ThemeId",
                table: "Boards",
                column: "ThemeId",
                principalTable: "Themes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PostIts_Boards_BoardId",
                table: "PostIts",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
