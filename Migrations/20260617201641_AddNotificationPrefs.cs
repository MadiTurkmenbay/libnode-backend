using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibNode.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPrefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNotificationPrefs",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnableCommentReply = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableTeamInvite = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableRequestApproved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableRequestRejected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableNewChapter = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableMention = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableLevelUp = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EnableAchievement = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationPrefs", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserNotificationPrefs_Users_UserId",
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
                name: "UserNotificationPrefs");
        }
    }
}
