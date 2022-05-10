using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class RelatedReprimandEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarningId",
                table: "ReprimandAction",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_WarningId",
                table: "ReprimandAction",
                column: "WarningId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_Warning_WarningId",
                table: "ReprimandAction",
                column: "WarningId",
                principalTable: "Warning",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_Warning_WarningId",
                table: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_WarningId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "WarningId",
                table: "ReprimandAction");
        }
    }
}