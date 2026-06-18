using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibNode.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserQuests",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    Rewarded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuests", x => new { x.UserId, x.QuestKey, x.Day });
                    table.ForeignKey(
                        name: "FK_UserQuests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserQuests");
        }
    }
}
