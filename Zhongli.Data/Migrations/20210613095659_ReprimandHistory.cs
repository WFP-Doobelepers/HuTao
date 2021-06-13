using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class ReprimandHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Guilds_GuildId",
                table: "Warning");

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Warning",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.CreateTable(
                name: "ReprimandAction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reprimand = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    GuildUserEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReprimandAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReprimandAction_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReprimandAction_Users_GuildUserEntityId",
                        column: x => x.GuildUserEntityId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReprimandAction_Users_ModeratorId",
                        column: x => x.ModeratorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReprimandAction_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_GuildId",
                table: "ReprimandAction",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_GuildUserEntityId",
                table: "ReprimandAction",
                column: "GuildUserEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_ModeratorId",
                table: "ReprimandAction",
                column: "ModeratorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_UserId",
                table: "ReprimandAction",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Guilds_GuildId",
                table: "Warning",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Guilds_GuildId",
                table: "Warning");

            migrationBuilder.DropTable(
                name: "ReprimandAction");

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Warning",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Guilds_GuildId",
                table: "Warning",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
