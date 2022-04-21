using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AuthorizationGroupModerationAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_Guilds_GuildId",
                table: "AuthorizationGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_Users_AddedById_GuildId",
                table: "AuthorizationGroup");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationGroup_AddedById_GuildId",
                table: "AuthorizationGroup");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationGroup_GuildId",
                table: "AuthorizationGroup");

            migrationBuilder.DropColumn(
                name: "AddedById",
                table: "AuthorizationGroup");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "AuthorizationGroup");

            migrationBuilder.AddColumn<Guid>(
                name: "ActionId",
                table: "AuthorizationGroup",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildEntityId",
                table: "AuthorizationGroup",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroup_ActionId",
                table: "AuthorizationGroup",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroup_GuildEntityId",
                table: "AuthorizationGroup",
                column: "GuildEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroup_Guilds_GuildEntityId",
                table: "AuthorizationGroup",
                column: "GuildEntityId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroup_ModerationAction_ActionId",
                table: "AuthorizationGroup",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_Guilds_GuildEntityId",
                table: "AuthorizationGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_ModerationAction_ActionId",
                table: "AuthorizationGroup");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationGroup_ActionId",
                table: "AuthorizationGroup");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationGroup_GuildEntityId",
                table: "AuthorizationGroup");

            migrationBuilder.DropColumn(
                name: "ActionId",
                table: "AuthorizationGroup");

            migrationBuilder.DropColumn(
                name: "GuildEntityId",
                table: "AuthorizationGroup");

            migrationBuilder.AddColumn<decimal>(
                name: "AddedById",
                table: "AuthorizationGroup",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "AuthorizationGroup",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroup_AddedById_GuildId",
                table: "AuthorizationGroup",
                columns: new[] { "AddedById", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroup_GuildId",
                table: "AuthorizationGroup",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroup_Guilds_GuildId",
                table: "AuthorizationGroup",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroup_Users_AddedById_GuildId",
                table: "AuthorizationGroup",
                columns: new[] { "AddedById", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}