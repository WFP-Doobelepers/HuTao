using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddCensorExclusions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ModerationRulesId",
                table: "Criterion",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Criterion_ModerationRulesId",
                table: "Criterion",
                column: "ModerationRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Criterion_ModerationRules_ModerationRulesId",
                table: "Criterion",
                column: "ModerationRulesId",
                principalTable: "ModerationRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Criterion_ModerationRules_ModerationRulesId",
                table: "Criterion");

            migrationBuilder.DropIndex(
                name: "IX_Criterion_ModerationRulesId",
                table: "Criterion");

            migrationBuilder.DropColumn(
                name: "ModerationRulesId",
                table: "Criterion");
        }
    }
}