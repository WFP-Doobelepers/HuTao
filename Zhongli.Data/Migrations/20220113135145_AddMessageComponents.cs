using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zhongli.Data.Migrations
{
    public partial class AddMessageComponents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionRow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageLogId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionRow_MessageLog_MessageLogId",
                        column: x => x.MessageLogId,
                        principalTable: "MessageLog",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Component",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    CustomId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionRowId = table.Column<Guid>(type: "uuid", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    Style = table.Column<int>(type: "integer", nullable: true),
                    Emote = table.Column<string>(type: "text", nullable: true),
                    Label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    MaxValues = table.Column<int>(type: "integer", nullable: true),
                    MinValues = table.Column<int>(type: "integer", nullable: true),
                    Placeholder = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Component", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Component_ActionRow_ActionRowId",
                        column: x => x.ActionRowId,
                        principalTable: "ActionRow",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MenuOption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: true),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Emote = table.Column<string>(type: "text", nullable: true),
                    SelectMenuId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuOption_Component_SelectMenuId",
                        column: x => x.SelectMenuId,
                        principalTable: "Component",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRow_MessageLogId",
                table: "ActionRow",
                column: "MessageLogId");

            migrationBuilder.CreateIndex(
                name: "IX_Component_ActionRowId",
                table: "Component",
                column: "ActionRowId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuOption_SelectMenuId",
                table: "MenuOption",
                column: "SelectMenuId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuOption");

            migrationBuilder.DropTable(
                name: "Component");

            migrationBuilder.DropTable(
                name: "ActionRow");
        }
    }
}
