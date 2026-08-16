using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrumMaster.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPollUtilite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeamsChannelId",
                table: "Equipes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PollsUtilite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaPath = table.Column<string>(type: "text", nullable: false),
                    TypeReunion = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateCloture = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollsUtilite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollsUtilite_Equipes_AreaPath",
                        column: x => x.AreaPath,
                        principalTable: "Equipes",
                        principalColumn: "AreaPath",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VotesUtilite",
                columns: table => new
                {
                    PollId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamsUserId = table.Column<string>(type: "text", nullable: false),
                    NomAffiche = table.Column<string>(type: "text", nullable: false),
                    Reponse = table.Column<int>(type: "integer", nullable: false),
                    DateVote = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotesUtilite", x => new { x.PollId, x.TeamsUserId });
                    table.ForeignKey(
                        name: "FK_VotesUtilite_PollsUtilite_PollId",
                        column: x => x.PollId,
                        principalTable: "PollsUtilite",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PollsUtilite_AreaPath_TypeReunion_Date",
                table: "PollsUtilite",
                columns: new[] { "AreaPath", "TypeReunion", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VotesUtilite");

            migrationBuilder.DropTable(
                name: "PollsUtilite");

            migrationBuilder.DropColumn(
                name: "TeamsChannelId",
                table: "Equipes");
        }
    }
}
