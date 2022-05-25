using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddHardMute : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HardMuteRoleId",
                table: "ModerationRules",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HardMuteRoleId",
                table: "ModerationCategory",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "HardMuteRoleEntity",
                columns: table => new
                {
                    MutesId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HardMuteRoleEntity", x => new { x.MutesId, x.RolesRoleId });
                    table.ForeignKey(
                        name: "FK_HardMuteRoleEntity_Reprimand_MutesId",
                        column: x => x.MutesId,
                        principalTable: "Reprimand",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HardMuteRoleEntity_Roles_RolesRoleId",
                        column: x => x.RolesRoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HardMuteRoleEntity_RolesRoleId",
                table: "HardMuteRoleEntity",
                column: "RolesRoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HardMuteRoleEntity");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropColumn(
                name: "HardMuteRoleId",
                table: "ModerationRules");

            migrationBuilder.DropColumn(
                name: "HardMuteRoleId",
                table: "ModerationCategory");
        }
    }
}
