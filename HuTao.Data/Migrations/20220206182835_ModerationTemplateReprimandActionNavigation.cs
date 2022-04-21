using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class ModerationTemplateReprimandActionNavigation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Count",
                table: "ModerationTemplate");

            migrationBuilder.DropColumn(
                name: "DeleteDays",
                table: "ModerationTemplate");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "ModerationTemplate");

            migrationBuilder.DropColumn(
                name: "Length",
                table: "ModerationTemplate");

            migrationBuilder.AddColumn<Guid>(
                name: "ActionId",
                table: "ModerationTemplate",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ModerationTemplate_ActionId",
                table: "ModerationTemplate",
                column: "ActionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ModerationTemplate_ReprimandAction_ActionId",
                table: "ModerationTemplate",
                column: "ActionId",
                principalTable: "ReprimandAction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModerationTemplate_ReprimandAction_ActionId",
                table: "ModerationTemplate");

            migrationBuilder.DropIndex(
                name: "IX_ModerationTemplate_ActionId",
                table: "ModerationTemplate");

            migrationBuilder.DropColumn(
                name: "ActionId",
                table: "ModerationTemplate");

            migrationBuilder.AddColumn<long>(
                name: "Count",
                table: "ModerationTemplate",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeleteDays",
                table: "ModerationTemplate",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "ModerationTemplate",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Length",
                table: "ModerationTemplate",
                type: "interval",
                nullable: true);
        }
    }
}