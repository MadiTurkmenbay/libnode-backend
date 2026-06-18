using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibNode.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChapterVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChapterVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "ru"),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterVersions_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterVersions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChapterVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChapterVersionVotes",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterVersionVotes", x => new { x.UserId, x.ChapterVersionId });
                    table.ForeignKey(
                        name: "FK_ChapterVersionVotes_ChapterVersions_ChapterVersionId",
                        column: x => x.ChapterVersionId,
                        principalTable: "ChapterVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterVersionVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterVersions_ChapterId_IsPublished",
                table: "ChapterVersions",
                columns: new[] { "ChapterId", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterVersions_CreatedByUserId",
                table: "ChapterVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterVersions_TeamId",
                table: "ChapterVersions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterVersionVotes_ChapterVersionId",
                table: "ChapterVersionVotes",
                column: "ChapterVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterVersionVotes");

            migrationBuilder.DropTable(
                name: "ChapterVersions");
        }
    }
}
