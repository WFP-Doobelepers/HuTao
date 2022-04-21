using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class CensorEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Censor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Pattern = table.Column<string>(type: "text", nullable: false),
                    Options = table.Column<int>(type: "integer", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AutoModerationRulesId = table.Column<Guid>(type: "uuid", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    DeleteDays = table.Column<long>(type: "bigint", nullable: true),
                    Length = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Censor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Censor_AutoModerationRules_AutoModerationRulesId",
                        column: x => x.AutoModerationRulesId,
                        principalTable: "AutoModerationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Censor_ModerationAction_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ModerationAction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Censor_ActionId",
                table: "Censor",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Censor_AutoModerationRulesId",
                table: "Censor",
                column: "AutoModerationRulesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Censor");
        }
    }
}