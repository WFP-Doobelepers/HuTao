using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class ReprimandHistoryCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_Guilds_GuildEntityId",
                table: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_GuildEntityId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "NoticeCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WarningCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GuildEntityId",
                table: "ReprimandAction");

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_GuildId",
                table: "ReprimandAction",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_Guilds_GuildId",
                table: "ReprimandAction",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_Guilds_GuildId",
                table: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_GuildId",
                table: "ReprimandAction");

            migrationBuilder.AddColumn<long>(
                name: "NoticeCount",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "WarningCount",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildEntityId",
                table: "ReprimandAction",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_GuildEntityId",
                table: "ReprimandAction",
                column: "GuildEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_Guilds_GuildEntityId",
                table: "ReprimandAction",
                column: "GuildEntityId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
