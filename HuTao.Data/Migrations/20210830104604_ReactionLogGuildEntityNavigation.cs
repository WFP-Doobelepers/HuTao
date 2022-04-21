using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class ReactionLogGuildEntityNavigation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReactionLog_Guilds_GuildEntityId",
                table: "ReactionLog");

            migrationBuilder.DropIndex(
                name: "IX_ReactionLog_GuildEntityId",
                table: "ReactionLog");

            migrationBuilder.DropColumn(
                name: "GuildEntityId",
                table: "ReactionLog");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionLog_GuildId",
                table: "ReactionLog",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReactionLog_Guilds_GuildId",
                table: "ReactionLog",
                column: "GuildId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReactionLog_Guilds_GuildId",
                table: "ReactionLog");

            migrationBuilder.DropIndex(
                name: "IX_ReactionLog_GuildId",
                table: "ReactionLog");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildEntityId",
                table: "ReactionLog",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReactionLog_GuildEntityId",
                table: "ReactionLog",
                column: "GuildEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReactionLog_Guilds_GuildEntityId",
                table: "ReactionLog",
                column: "GuildEntityId",
                principalTable: "Guilds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}