using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddTemporaryRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemporaryRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpireAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemporaryRole_Guilds_GuildEntityId",
                        column: x => x.GuildEntityId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemporaryRole_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryRole_ActionId",
                table: "TemporaryRole",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryRole_GuildEntityId",
                table: "TemporaryRole",
                column: "GuildEntityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemporaryRole");
        }
    }
}