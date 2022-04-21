using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class GuildEntityForeignKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_AuthorizationRules_AuthorizationRulesId",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_AutoModerationRules_AutoModerationRulesId",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_Users_AddedById_AddedByGuildId",
                table: "UserAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthorization_AddedById_AddedByGuildId",
                table: "UserAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_AuthorizationRulesId",
                table: "Guilds");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_AutoModerationRulesId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "AddedByGuildId",
                table: "UserAuthorization");

            migrationBuilder.DropColumn(
                name: "AuthorizationRulesId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "AutoModerationRulesId",
                table: "Guilds");

            migrationBuilder.AlterColumn<decimal>(
                name: "AddedById",
                table: "UserAuthorization",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "RoleAuthorization",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "PermissionAuthorization",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "ChannelAuthorization",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "AutoModerationRules",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "AuthorizationRules",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "AntiSpamRules",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorization_AddedById_GuildId",
                table: "UserAuthorization",
                columns: new[] { "AddedById", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorization_GuildId",
                table: "UserAuthorization",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorization_UserId_GuildId",
                table: "UserAuthorization",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAuthorization_GuildId",
                table: "RoleAuthorization",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuthorization_GuildId",
                table: "PermissionAuthorization",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildAuthorization_GuildId",
                table: "GuildAuthorization",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAuthorization_GuildId",
                table: "ChannelAuthorization",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoModerationRules_GuildId",
                table: "AutoModerationRules",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRules_GuildId",
                table: "AuthorizationRules",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AntiSpamRules_GuildId",
                table: "AntiSpamRules",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_AntiSpamRules_Guilds_GuildId",
                table: "AntiSpamRules",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRules_Guilds_GuildId",
                table: "AuthorizationRules",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AutoModerationRules_Guilds_GuildId",
                table: "AutoModerationRules",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelAuthorization_Guilds_GuildId",
                table: "ChannelAuthorization",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GuildAuthorization_Guilds_GuildId",
                table: "GuildAuthorization",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionAuthorization_Guilds_GuildId",
                table: "PermissionAuthorization",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAuthorization_Guilds_GuildId",
                table: "RoleAuthorization",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_Guilds_GuildId",
                table: "UserAuthorization",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_Users_AddedById_GuildId",
                table: "UserAuthorization",
                columns: new[] { "AddedById", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_Users_UserId_GuildId",
                table: "UserAuthorization",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AntiSpamRules_Guilds_GuildId",
                table: "AntiSpamRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRules_Guilds_GuildId",
                table: "AuthorizationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AutoModerationRules_Guilds_GuildId",
                table: "AutoModerationRules");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelAuthorization_Guilds_GuildId",
                table: "ChannelAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_GuildAuthorization_Guilds_GuildId",
                table: "GuildAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionAuthorization_Guilds_GuildId",
                table: "PermissionAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleAuthorization_Guilds_GuildId",
                table: "RoleAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_Guilds_GuildId",
                table: "UserAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_Users_AddedById_GuildId",
                table: "UserAuthorization");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAuthorization_Users_UserId_GuildId",
                table: "UserAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthorization_AddedById_GuildId",
                table: "UserAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthorization_GuildId",
                table: "UserAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthorization_UserId_GuildId",
                table: "UserAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_RoleAuthorization_GuildId",
                table: "RoleAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_PermissionAuthorization_GuildId",
                table: "PermissionAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_GuildAuthorization_GuildId",
                table: "GuildAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_ChannelAuthorization_GuildId",
                table: "ChannelAuthorization");

            migrationBuilder.DropIndex(
                name: "IX_AutoModerationRules_GuildId",
                table: "AutoModerationRules");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationRules_GuildId",
                table: "AuthorizationRules");

            migrationBuilder.DropIndex(
                name: "IX_AntiSpamRules_GuildId",
                table: "AntiSpamRules");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "RoleAuthorization");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "PermissionAuthorization");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "ChannelAuthorization");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "AutoModerationRules");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "AuthorizationRules");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "AntiSpamRules");

            migrationBuilder.AlterColumn<decimal>(
                name: "AddedById",
                table: "UserAuthorization",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddColumn<decimal>(
                name: "AddedByGuildId",
                table: "UserAuthorization",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorizationRulesId",
                table: "Guilds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AutoModerationRulesId",
                table: "Guilds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthorization_AddedById_AddedByGuildId",
                table: "UserAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_AuthorizationRulesId",
                table: "Guilds",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_AutoModerationRulesId",
                table: "Guilds",
                column: "AutoModerationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_AuthorizationRules_AuthorizationRulesId",
                table: "Guilds",
                column: "AuthorizationRulesId",
                principalTable: "AuthorizationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_AutoModerationRules_AutoModerationRulesId",
                table: "Guilds",
                column: "AutoModerationRulesId",
                principalTable: "AutoModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuthorization_Users_AddedById_AddedByGuildId",
                table: "UserAuthorization",
                columns: new[] { "AddedById", "AddedByGuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}