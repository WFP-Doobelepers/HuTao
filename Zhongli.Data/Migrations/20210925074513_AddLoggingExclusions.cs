using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class AddLoggingExclusions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LoggingRulesId",
                table: "Criterion",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Criterion_LoggingRulesId",
                table: "Criterion",
                column: "LoggingRulesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Criterion_LoggingRules_LoggingRulesId",
                table: "Criterion",
                column: "LoggingRulesId",
                principalTable: "LoggingRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Criterion_LoggingRules_LoggingRulesId",
                table: "Criterion");

            migrationBuilder.DropIndex(
                name: "IX_Criterion_LoggingRulesId",
                table: "Criterion");

            migrationBuilder.DropColumn(
                name: "LoggingRulesId",
                table: "Criterion");
        }
    }
}
