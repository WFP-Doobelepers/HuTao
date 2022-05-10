using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AuthorizationRulesToGrouping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_AuthorizationRule_AuthorizationGroupId",
                table: "AuthorizationRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_AuthorizationRules_AuthorizationRulesId",
                table: "AuthorizationRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_Guilds_GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_Users_AddedById_GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_Users_UserId_GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropTable(
                name: "AuthorizationRules");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationRule_AddedById_GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationRule_AuthorizationRulesId",
                table: "AuthorizationRule");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationRule_GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationRule_UserId_GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "AddedById",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "AuthorizationRulesId",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "AuthorizationRule");

            migrationBuilder.CreateTable(
                name: "AuthorizationGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AddedById = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorizationGroup_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorizationGroup_Users_AddedById_GuildId",
                        columns: x => new { x.AddedById, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroup_AddedById_GuildId",
                table: "AuthorizationGroup",
                columns: new[] { "AddedById", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroup_GuildId",
                table: "AuthorizationGroup",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_AuthorizationGroup_AuthorizationGroupId",
                table: "AuthorizationRule",
                column: "AuthorizationGroupId",
                principalTable: "AuthorizationGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_AuthorizationGroup_AuthorizationGroupId",
                table: "AuthorizationRule");

            migrationBuilder.DropTable(
                name: "AuthorizationGroup");

            migrationBuilder.AddColumn<decimal>(
                name: "AddedById",
                table: "AuthorizationRule",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorizationRulesId",
                table: "AuthorizationRule",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "AuthorizationRule",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "AuthorizationRule",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuthorizationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorizationRules_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRule_AddedById_GuildId",
                table: "AuthorizationRule",
                columns: new[] { "AddedById", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRule_AuthorizationRulesId",
                table: "AuthorizationRule",
                column: "AuthorizationRulesId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRule_GuildId",
                table: "AuthorizationRule",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRule_UserId_GuildId",
                table: "AuthorizationRule",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRules_GuildId",
                table: "AuthorizationRules",
                column: "GuildId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_AuthorizationRule_AuthorizationGroupId",
                table: "AuthorizationRule",
                column: "AuthorizationGroupId",
                principalTable: "AuthorizationRule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_AuthorizationRules_AuthorizationRulesId",
                table: "AuthorizationRule",
                column: "AuthorizationRulesId",
                principalTable: "AuthorizationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_Guilds_GuildId",
                table: "AuthorizationRule",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_Users_AddedById_GuildId",
                table: "AuthorizationRule",
                columns: new[] { "AddedById", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_Users_UserId_GuildId",
                table: "AuthorizationRule",
                columns: new[] { "UserId", "GuildId" },
                principalTable: "Users",
                principalColumns: new[] { "Id", "GuildId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}