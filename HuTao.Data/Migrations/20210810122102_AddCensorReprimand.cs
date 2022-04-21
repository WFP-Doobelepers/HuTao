using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddCensorReprimand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CensorId",
                table: "ReprimandAction",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReprimandAction_CensorId",
                table: "ReprimandAction",
                column: "CensorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReprimandAction_Censor_CensorId",
                table: "ReprimandAction",
                column: "CensorId",
                principalTable: "Censor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReprimandAction_Censor_CensorId",
                table: "ReprimandAction");

            migrationBuilder.DropIndex(
                name: "IX_ReprimandAction_CensorId",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "CensorId",
                table: "ReprimandAction");
        }
    }
}