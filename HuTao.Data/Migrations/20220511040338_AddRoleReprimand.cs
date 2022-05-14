using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddRoleReprimand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoleActionId",
                table: "RoleTemplate",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleReprimandId",
                table: "RoleTemplate",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplate_RoleActionId",
                table: "RoleTemplate",
                column: "RoleActionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTemplate_RoleReprimandId",
                table: "RoleTemplate",
                column: "RoleReprimandId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoleTemplate_Reprimand_RoleReprimandId",
                table: "RoleTemplate",
                column: "RoleReprimandId",
                principalTable: "Reprimand",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoleTemplate_ReprimandAction_RoleActionId",
                table: "RoleTemplate",
                column: "RoleActionId",
                principalTable: "ReprimandAction",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoleTemplate_Reprimand_RoleReprimandId",
                table: "RoleTemplate");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleTemplate_ReprimandAction_RoleActionId",
                table: "RoleTemplate");

            migrationBuilder.DropIndex(
                name: "IX_RoleTemplate_RoleActionId",
                table: "RoleTemplate");

            migrationBuilder.DropIndex(
                name: "IX_RoleTemplate_RoleReprimandId",
                table: "RoleTemplate");

            migrationBuilder.DropColumn(
                name: "RoleActionId",
                table: "RoleTemplate");

            migrationBuilder.DropColumn(
                name: "RoleReprimandId",
                table: "RoleTemplate");
        }
    }
}
