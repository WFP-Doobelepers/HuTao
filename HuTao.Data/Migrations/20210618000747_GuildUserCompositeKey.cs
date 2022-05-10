using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class GuildUserCompositeKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ban_Users_UserId",
                table: "Ban");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelAuthorization_Users_AddedById",
                table: "ChannelAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_GuildAuthorization_Users_AddedById",
                table: "GuildAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_Kick_Users_UserId",
                table: "Kick");

            migrationBuilder.DropForeignKey(
                name: "FK_Mute_Users_UserId",
                table: "Mute");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionAuthorization_Users_AddedById",
                table: "PermissionAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleAuthorization_Users_AddedById",
                table: "RoleAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_Users_AddedById",
                table: "UserAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Users_UserId",
                table: "Warning");

            migrationBuilder.DropIndex(
                name: "IX_Warning_UserId",
                table: "Warning");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthorization_AddedById",
                table: "UserAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_RoleAuthorization_AddedById",
                table: "RoleAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_PermissionAuthorization_AddedById",
                table: "PermissionAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_Mute_UserId",
                table: "Mute");

            migrationBuilder.DropIndex(
                name: "IX_Kick_UserId",
                table: "Kick");

            migrationBuilder.DropIndex(
                name: "IX_GuildAuthorization_AddedById",
                table: "GuildAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_ChannelAuthorization_AddedById",
                table: "ChannelAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_Ban_UserId",
                table: "Ban");

            migrationBuilder.AddColumn<decimal>(
                name: "AddedByGuildId",
                table: "UserAuthorization",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AddedByGuildId",
                table: "RoleAuthorization",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AddedByGuildId",
                table: "PermissionAuthorization",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AddedByGuildId",
                table: "GuildAuthorization",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AddedByGuildId",
                table: "ChannelAuthorization",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                columns: new[] { "Id", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Warning_GuildId",
                table: "Warning",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_ModeratorId_GuildId",
                table: "Warning",
                columns: new[] { "ModeratorId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Warning_UserId_GuildId",
                table: "Warning",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorization_AddedById_AddedByGuildId",
                table: "UserAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuthorization_AddedById_AddedByGuildId",
                table: "RoleAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuthorization_AddedById_AddedByGuildId",
                table: "PermissionAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Mute_GuildId",
                table: "Mute",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Mute_ModeratorId_GuildId",
                table: "Mute",
                columns: new[] { "ModeratorId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Mute_UserId_GuildId",
                table: "Mute",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Kick_GuildId",
                table: "Kick",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Kick_ModeratorId_GuildId",
                table: "Kick",
                columns: new[] { "ModeratorId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Kick_UserId_GuildId",
                table: "Kick",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_GuildAuthorization_AddedById_AddedByGuildId",
                table: "GuildAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuthorization_AddedById_AddedByGuildId",
                table: "ChannelAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Ban_GuildId",
                table: "Ban",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Ban_ModeratorId_GuildId",
                table: "Ban",
                columns: new[] { "ModeratorId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Ban_UserId_GuildId",
                table: "Ban",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Ban_Guilds_GuildId",
                table: "Ban",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ban_Users_ModeratorId_GuildId",
                table: "Ban",
                columns: new[] { "ModeratorId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ban_Users_UserId_GuildId",
                table: "Ban",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelAuthorization_Users_AddedById_AddedByGuildId",
                table: "ChannelAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GuildAuthorization_Users_AddedById_AddedByGuildId",
                table: "GuildAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kick_Guilds_GuildId",
                table: "Kick",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Kick_Users_ModeratorId_GuildId",
                table: "Kick",
                columns: new[] { "ModeratorId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Kick_Users_UserId_GuildId",
                table: "Kick",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mute_Guilds_GuildId",
                table: "Mute",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mute_Users_ModeratorId_GuildId",
                table: "Mute",
                columns: new[] { "ModeratorId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mute_Users_UserId_GuildId",
                table: "Mute",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionAuthorization_Users_AddedById_AddedByGuildId",
                table: "PermissionAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAuthorization_Users_AddedById_AddedByGuildId",
                table: "RoleAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_Users_AddedById_AddedByGuildId",
                table: "UserAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Guilds_GuildId",
                table: "Warning",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Users_ModeratorId_GuildId",
                table: "Warning",
                columns: new[] { "ModeratorId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Users_UserId_GuildId",
                table: "Warning",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ban_Guilds_GuildId",
                table: "Ban");

            migrationBuilder.DropForeignKey(
                name: "FK_Ban_Users_ModeratorId_GuildId",
                table: "Ban");

            migrationBuilder.DropForeignKey(
                name: "FK_Ban_Users_UserId_GuildId",
                table: "Ban");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelAuthorization_Users_AddedById_AddedByGuildId",
                table: "ChannelAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_GuildAuthorization_Users_AddedById_AddedByGuildId",
                table: "GuildAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_Kick_Guilds_GuildId",
                table: "Kick");

            migrationBuilder.DropForeignKey(
                name: "FK_Kick_Users_ModeratorId_GuildId",
                table: "Kick");

            migrationBuilder.DropForeignKey(
                name: "FK_Kick_Users_UserId_GuildId",
                table: "Kick");

            migrationBuilder.DropForeignKey(
                name: "FK_Mute_Guilds_GuildId",
                table: "Mute");

            migrationBuilder.DropForeignKey(
                name: "FK_Mute_Users_ModeratorId_GuildId",
                table: "Mute");

            migrationBuilder.DropForeignKey(
                name: "FK_Mute_Users_UserId_GuildId",
                table: "Mute");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionAuthorization_Users_AddedById_AddedByGuildId",
                table: "PermissionAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleAuthorization_Users_AddedById_AddedByGuildId",
                table: "RoleAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_Users_AddedById_AddedByGuildId",
                table: "UserAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Guilds_GuildId",
                table: "Warning");

            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Users_ModeratorId_GuildId",
                table: "Warning");

            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Users_UserId_GuildId",
                table: "Warning");

            migrationBuilder.DropIndex(
                name: "IX_Warning_GuildId",
                table: "Warning");

            migrationBuilder.DropIndex(
                name: "IX_Warning_ModeratorId_GuildId",
                table: "Warning");

            migrationBuilder.DropIndex(
                name: "IX_Warning_UserId_GuildId",
                table: "Warning");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthorization_AddedById_AddedByGuildId",
                table: "UserAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_RoleAuthorization_AddedById_AddedByGuildId",
                table: "RoleAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_PermissionAuthorization_AddedById_AddedByGuildId",
                table: "PermissionAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_Mute_GuildId",
                table: "Mute");

            migrationBuilder.DropIndex(
                name: "IX_Mute_ModeratorId_GuildId",
                table: "Mute");

            migrationBuilder.DropIndex(
                name: "IX_Mute_UserId_GuildId",
                table: "Mute");

            migrationBuilder.DropIndex(
                name: "IX_Kick_GuildId",
                table: "Kick");

            migrationBuilder.DropIndex(
                name: "IX_Kick_ModeratorId_GuildId",
                table: "Kick");

            migrationBuilder.DropIndex(
                name: "IX_Kick_UserId_GuildId",
                table: "Kick");

            migrationBuilder.DropIndex(
                name: "IX_GuildAuthorization_AddedById_AddedByGuildId",
                table: "GuildAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_ChannelAuthorization_AddedById_AddedByGuildId",
                table: "ChannelAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_Ban_GuildId",
                table: "Ban");

            migrationBuilder.DropIndex(
                name: "IX_Ban_ModeratorId_GuildId",
                table: "Ban");

            migrationBuilder.DropIndex(
                name: "IX_Ban_UserId_GuildId",
                table: "Ban");

            migrationBuilder.DropColumn(
                name: "AddedByGuildId",
                table: "UserAuthorization");

            migrationBuilder.DropColumn(
                name: "AddedByGuildId",
                table: "RoleAuthorization");

            migrationBuilder.DropColumn(
                name: "AddedByGuildId",
                table: "PermissionAuthorization");

            migrationBuilder.DropColumn(
                name: "AddedByGuildId",
                table: "GuildAuthorization");

            migrationBuilder.DropColumn(
                name: "AddedByGuildId",
                table: "ChannelAuthorization");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_UserId",
                table: "Warning",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorization_AddedById",
                table: "UserAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuthorization_AddedById",
                table: "RoleAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuthorization_AddedById",
                table: "PermissionAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_Mute_UserId",
                table: "Mute",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Kick_UserId",
                table: "Kick",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildAuthorization_AddedById",
                table: "GuildAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuthorization_AddedById",
                table: "ChannelAuthorization",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_Ban_UserId",
                table: "Ban",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ban_Users_UserId",
                table: "Ban",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelAuthorization_Users_AddedById",
                table: "ChannelAuthorization",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GuildAuthorization_Users_AddedById",
                table: "GuildAuthorization",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kick_Users_UserId",
                table: "Kick",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mute_Users_UserId",
                table: "Mute",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionAuthorization_Users_AddedById",
                table: "PermissionAuthorization",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAuthorization_Users_AddedById",
                table: "RoleAuthorization",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_Users_AddedById",
                table: "UserAuthorization",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Users_UserId",
                table: "Warning",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}