using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class ReprimandHistoryDenormalization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ban_Guilds_GuildId",
                table: "Ban");

            migrationBuilder.DropForeignKey(
                name: "FK_Ban_Users_GuildUserEntityId",
                table: "Ban");

            migrationBuilder.DropForeignKey(
                name: "FK_Ban_Users_ModeratorId",
                table: "Ban");

            migrationBuilder.DropForeignKey(
                name: "FK_Ban_Users_UserId",
                table: "Ban");

            migrationBuilder.DropForeignKey(
                name: "FK_Kick_Guilds_GuildId",
                table: "Kick");

            migrationBuilder.DropForeignKey(
                name: "FK_Kick_Users_GuildUserEntityId",
                table: "Kick");

            migrationBuilder.DropForeignKey(
                name: "FK_Kick_Users_ModeratorId",
                table: "Kick");

            migrationBuilder.DropForeignKey(
                name: "FK_Kick_Users_UserId",
                table: "Kick");

            migrationBuilder.DropForeignKey(
                name: "FK_Mute_Guilds_GuildId",
                table: "Mute");

            migrationBuilder.DropForeignKey(
                name: "FK_Mute_Users_GuildUserEntityId",
                table: "Mute");

            migrationBuilder.DropForeignKey(
                name: "FK_Mute_Users_ModeratorId",
                table: "Mute");

            migrationBuilder.DropForeignKey(
                name: "FK_Mute_Users_UserId",
                table: "Mute");

            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Guilds_GuildId",
                table: "Warning");

            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Users_GuildUserEntityId",
                table: "Warning");

            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Users_ModeratorId",
                table: "Warning");

            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Users_UserId",
                table: "Warning");

            migrationBuilder.DropTable(
                name: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_Warning_GuildId",
                table: "Warning");

            migrationBuilder.DropIndex(
                name: "IX_Warning_GuildUserEntityId",
                table: "Warning");

            migrationBuilder.DropIndex(
                name: "IX_Warning_ModeratorId",
                table: "Warning");

            migrationBuilder.DropIndex(
                name: "IX_Mute_GuildId",
                table: "Mute");

            migrationBuilder.DropIndex(
                name: "IX_Mute_GuildUserEntityId",
                table: "Mute");

            migrationBuilder.DropIndex(
                name: "IX_Mute_ModeratorId",
                table: "Mute");

            migrationBuilder.DropIndex(
                name: "IX_Kick_GuildId",
                table: "Kick");

            migrationBuilder.DropIndex(
                name: "IX_Kick_GuildUserEntityId",
                table: "Kick");

            migrationBuilder.DropIndex(
                name: "IX_Kick_ModeratorId",
                table: "Kick");

            migrationBuilder.DropIndex(
                name: "IX_Ban_GuildId",
                table: "Ban");

            migrationBuilder.DropIndex(
                name: "IX_Ban_GuildUserEntityId",
                table: "Ban");

            migrationBuilder.DropIndex(
                name: "IX_Ban_ModeratorId",
                table: "Ban");

            migrationBuilder.DropColumn(
                name: "GuildUserEntityId",
                table: "Warning");

            migrationBuilder.DropColumn(
                name: "GuildUserEntityId",
                table: "Mute");

            migrationBuilder.DropColumn(
                name: "GuildUserEntityId",
                table: "Kick");

            migrationBuilder.DropColumn(
                name: "GuildUserEntityId",
                table: "Ban");

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "Warning",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ModeratorId",
                table: "Warning",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Warning",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "Mute",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ModeratorId",
                table: "Mute",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Mute",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "Kick",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ModeratorId",
                table: "Kick",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Kick",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "Ban",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ModeratorId",
                table: "Ban",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Ban",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Ban_Users_UserId",
                table: "Ban",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_Warning_Users_UserId",
                table: "Warning",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ban_Users_UserId",
                table: "Ban");

            migrationBuilder.DropForeignKey(
                name: "FK_Kick_Users_UserId",
                table: "Kick");

            migrationBuilder.DropForeignKey(
                name: "FK_Mute_Users_UserId",
                table: "Mute");

            migrationBuilder.DropForeignKey(
                name: "FK_Warning_Users_UserId",
                table: "Warning");

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "Warning",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ModeratorId",
                table: "Warning",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Warning",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildUserEntityId",
                table: "Warning",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "Mute",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ModeratorId",
                table: "Mute",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Mute",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildUserEntityId",
                table: "Mute",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "Kick",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ModeratorId",
                table: "Kick",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Kick",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildUserEntityId",
                table: "Kick",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "Ban",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ModeratorId",
                table: "Ban",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Ban",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildUserEntityId",
                table: "Ban",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReprimandAction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    GuildUserEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    ModeratorId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Reprimand = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    WarningId = table.Column<Guid>(type: "uuid", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_ReprimandAction_Warning_WarningId",
                        column: x => x.WarningId,
                        principalTable: "Warning",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warning_GuildId",
                table: "Warning",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_GuildUserEntityId",
                table: "Warning",
                column: "GuildUserEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Warning_ModeratorId",
                table: "Warning",
                column: "ModeratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Mute_GuildId",
                table: "Mute",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Mute_GuildUserEntityId",
                table: "Mute",
                column: "GuildUserEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Mute_ModeratorId",
                table: "Mute",
                column: "ModeratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Kick_GuildId",
                table: "Kick",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Kick_GuildUserEntityId",
                table: "Kick",
                column: "GuildUserEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Kick_ModeratorId",
                table: "Kick",
                column: "ModeratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Ban_GuildId",
                table: "Ban",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Ban_GuildUserEntityId",
                table: "Ban",
                column: "GuildUserEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Ban_ModeratorId",
                table: "Ban",
                column: "ModeratorId");

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

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_WarningId",
                table: "ReprimandAction",
                column: "WarningId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ban_Guilds_GuildId",
                table: "Ban",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ban_Users_GuildUserEntityId",
                table: "Ban",
                column: "GuildUserEntityId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ban_Users_ModeratorId",
                table: "Ban",
                column: "ModeratorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ban_Users_UserId",
                table: "Ban",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kick_Guilds_GuildId",
                table: "Kick",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kick_Users_GuildUserEntityId",
                table: "Kick",
                column: "GuildUserEntityId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kick_Users_ModeratorId",
                table: "Kick",
                column: "ModeratorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Kick_Users_UserId",
                table: "Kick",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mute_Guilds_GuildId",
                table: "Mute",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mute_Users_GuildUserEntityId",
                table: "Mute",
                column: "GuildUserEntityId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mute_Users_ModeratorId",
                table: "Mute",
                column: "ModeratorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mute_Users_UserId",
                table: "Mute",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Guilds_GuildId",
                table: "Warning",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Users_GuildUserEntityId",
                table: "Warning",
                column: "GuildUserEntityId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Users_ModeratorId",
                table: "Warning",
                column: "ModeratorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Warning_Users_UserId",
                table: "Warning",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}