using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class CriterionModerationActionRemoval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Criterion_ModerationAction_ActionId",
                table: "Criterion");

            migrationBuilder.DropIndex(
                name: "IX_Criterion_ActionId",
                table: "Criterion");

            migrationBuilder.DropColumn(
                name: "ActionId",
                table: "Criterion");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActionId",
                table: "Criterion",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Criterion_ActionId",
                table: "Criterion",
                column: "ActionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Criterion_ModerationAction_ActionId",
                table: "Criterion",
                column: "ActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
