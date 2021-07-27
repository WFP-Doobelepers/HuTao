using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class AddReprimandStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ReprimandAction");

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedActionId",
                table: "ReprimandAction",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ReprimandAction",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ModerationAction",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_ModifiedActionId",
                table: "ReprimandAction",
                column: "ModifiedActionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_ModerationAction_ModifiedActionId",
                table: "ReprimandAction",
                column: "ModifiedActionId",
                principalTable: "ModerationAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_ModerationAction_ModifiedActionId",
                table: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_ModifiedActionId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "ModifiedActionId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ModerationAction");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ReprimandAction",
                type: "text",
                nullable: true);
        }
    }
}
