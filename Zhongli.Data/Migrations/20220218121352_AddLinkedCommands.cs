using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zhongli.Data.Migrations
{
    public partial class AddLinkedCommands : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedCommandId",
                table: "RoleTemplate",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedCommandId",
                table: "Criterion",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LinkedCommand",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Ephemeral = table.Column<bool>(type: "boolean", nullable: false),
                    Silent = table.Column<bool>(type: "boolean", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Cooldown = table.Column<TimeSpan>(type: "interval", nullable: true),
                    UserOptions = table.Column<int>(type: "integer", nullable: false),
                    GuildEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedCommand", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkedCommand_Guilds_GuildEntityId",
                        column: x => x.GuildEntityId,
                        principalTable: "Guilds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LinkedCommand_MessageTemplate_MessageId",
                        column: x => x.MessageId,
                        principalTable: "MessageTemplate",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplate_LinkedCommandId",
                table: "RoleTemplate",
                column: "LinkedCommandId");

            migrationBuilder.CreateIndex(
                name: "IX_Criterion_LinkedCommandId",
                table: "Criterion",
                column: "LinkedCommandId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedCommand_GuildEntityId",
                table: "LinkedCommand",
                column: "GuildEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedCommand_MessageId",
                table: "LinkedCommand",
                column: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Criterion_LinkedCommand_LinkedCommandId",
                table: "Criterion",
                column: "LinkedCommandId",
                principalTable: "LinkedCommand",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoleTemplate_LinkedCommand_LinkedCommandId",
                table: "RoleTemplate",
                column: "LinkedCommandId",
                principalTable: "LinkedCommand",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Criterion_LinkedCommand_LinkedCommandId",
                table: "Criterion");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleTemplate_LinkedCommand_LinkedCommandId",
                table: "RoleTemplate");

            migrationBuilder.DropTable(
                name: "LinkedCommand");

            migrationBuilder.DropIndex(
                name: "IX_RoleTemplate_LinkedCommandId",
                table: "RoleTemplate");

            migrationBuilder.DropIndex(
                name: "IX_Criterion_LinkedCommandId",
                table: "Criterion");

            migrationBuilder.DropColumn(
                name: "LinkedCommandId",
                table: "RoleTemplate");

            migrationBuilder.DropColumn(
                name: "LinkedCommandId",
                table: "Criterion");
        }
    }
}
