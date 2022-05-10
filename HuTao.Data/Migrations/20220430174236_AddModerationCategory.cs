using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddModerationCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Trigger",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Reprimand",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "ModerationTemplate",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModerationCategoryId",
                table: "AuthorizationGroup",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModerationCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    GuildEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationCategory_Guilds_GuildEntityId",
                        column: x => x.GuildEntityId,
                        principalTable: "Guilds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trigger_CategoryId",
                table: "Trigger",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Reprimand_CategoryId",
                table: "Reprimand",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationTemplate_CategoryId",
                table: "ModerationTemplate",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroup_ModerationCategoryId",
                table: "AuthorizationGroup",
                column: "ModerationCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationCategory_GuildEntityId",
                table: "ModerationCategory",
                column: "GuildEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroup_ModerationCategory_ModerationCategoryId",
                table: "AuthorizationGroup",
                column: "ModerationCategoryId",
                principalTable: "ModerationCategory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationTemplate_ModerationCategory_CategoryId",
                table: "ModerationTemplate",
                column: "CategoryId",
                principalTable: "ModerationCategory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reprimand_ModerationCategory_CategoryId",
                table: "Reprimand",
                column: "CategoryId",
                principalTable: "ModerationCategory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Trigger_ModerationCategory_CategoryId",
                table: "Trigger",
                column: "CategoryId",
                principalTable: "ModerationCategory",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_ModerationCategory_ModerationCategoryId",
                table: "AuthorizationGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_ModerationTemplate_ModerationCategory_CategoryId",
                table: "ModerationTemplate");

            migrationBuilder.DropForeignKey(
                name: "FK_Reprimand_ModerationCategory_CategoryId",
                table: "Reprimand");

            migrationBuilder.DropForeignKey(
                name: "FK_Trigger_ModerationCategory_CategoryId",
                table: "Trigger");

            migrationBuilder.DropTable(
                name: "ModerationCategory");

            migrationBuilder.DropIndex(
                name: "IX_Trigger_CategoryId",
                table: "Trigger");

            migrationBuilder.DropIndex(
                name: "IX_Reprimand_CategoryId",
                table: "Reprimand");

            migrationBuilder.DropIndex(
                name: "IX_ModerationTemplate_CategoryId",
                table: "ModerationTemplate");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationGroup_ModerationCategoryId",
                table: "AuthorizationGroup");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Trigger");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Reprimand");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ModerationTemplate");

            migrationBuilder.DropColumn(
                name: "ModerationCategoryId",
                table: "AuthorizationGroup");
        }
    }
}
