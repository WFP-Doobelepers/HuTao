using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zhongli.Data.Migrations
{
    public partial class AddLinkedButtons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LinkedButton",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ephemeral = table.Column<bool>(type: "boolean", nullable: false),
                    ButtonId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkedButton", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinkedButton_Component_ButtonId",
                        column: x => x.ButtonId,
                        principalTable: "Component",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LinkedButton_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LinkedButton_MessageTemplate_MessageId",
                        column: x => x.MessageId,
                        principalTable: "MessageTemplate",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoleTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Behavior = table.Column<int>(type: "integer", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LinkedButtonId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleTemplate_LinkedButton_LinkedButtonId",
                        column: x => x.LinkedButtonId,
                        principalTable: "LinkedButton",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinkedButton_ButtonId",
                table: "LinkedButton",
                column: "ButtonId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedButton_GuildId",
                table: "LinkedButton",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_LinkedButton_MessageId",
                table: "LinkedButton",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplate_LinkedButtonId",
                table: "RoleTemplate",
                column: "LinkedButtonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleTemplate");

            migrationBuilder.DropTable(
                name: "LinkedButton");
        }
    }
}
