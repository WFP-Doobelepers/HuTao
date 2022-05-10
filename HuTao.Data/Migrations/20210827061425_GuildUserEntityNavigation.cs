using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class GuildUserEntityNavigation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageLog_Guilds_GuildEntityId",
                table: "MessageLog");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationAction_Users_ModeratorId_GuildId",
                table: "ModerationAction");

            migrationBuilder.DropForeignKey(
                name: "FK_VoiceChatLink_Users_OwnerId_GuildId",
                table: "VoiceChatLink");

            migrationBuilder.DropIndex(
                name: "IX_MessageLog_GuildEntityId",
                table: "MessageLog");

            migrationBuilder.DropColumn(
                name: "GuildEntityId",
                table: "MessageLog");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "VoiceChatLink",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_VoiceChatLink_OwnerId_GuildId",
                table: "VoiceChatLink",
                newName: "IX_VoiceChatLink_UserId_GuildId");

            migrationBuilder.RenameColumn(
                name: "ModeratorId",
                table: "ModerationAction",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ModerationAction_ModeratorId_GuildId",
                table: "ModerationAction",
                newName: "IX_ModerationAction_UserId_GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationAction_Users_UserId_GuildId",
                table: "ModerationAction",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VoiceChatLink_Users_UserId_GuildId",
                table: "VoiceChatLink",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModerationAction_Users_UserId_GuildId",
                table: "ModerationAction");

            migrationBuilder.DropForeignKey(
                name: "FK_VoiceChatLink_Users_UserId_GuildId",
                table: "VoiceChatLink");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "VoiceChatLink",
                newName: "OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_VoiceChatLink_UserId_GuildId",
                table: "VoiceChatLink",
                newName: "IX_VoiceChatLink_OwnerId_GuildId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ModerationAction",
                newName: "ModeratorId");

            migrationBuilder.RenameIndex(
                name: "IX_ModerationAction_UserId_GuildId",
                table: "ModerationAction",
                newName: "IX_ModerationAction_ModeratorId_GuildId");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildEntityId",
                table: "MessageLog",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageLog_GuildEntityId",
                table: "MessageLog",
                column: "GuildEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageLog_Guilds_GuildEntityId",
                table: "MessageLog",
                column: "GuildEntityId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationAction_Users_ModeratorId_GuildId",
                table: "ModerationAction",
                columns: new[] { "ModeratorId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VoiceChatLink_Users_OwnerId_GuildId",
                table: "VoiceChatLink",
                columns: new[] { "OwnerId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}