using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddModerationVariables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModerationVariable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    ModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationVariable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationVariable_ModerationRules_ModerationRulesId",
                        column: x => x.ModerationRulesId,
                        principalTable: "ModerationRules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationVariable_ModerationRulesId",
                table: "ModerationVariable",
                column: "ModerationRulesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModerationVariable");
        }
    }
}
