using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddReprimandContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContextId",
                table: "Reprimand",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reprimand_ContextId",
                table: "Reprimand",
                column: "ContextId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reprimand_MessageLog_ContextId",
                table: "Reprimand",
                column: "ContextId",
                principalTable: "MessageLog",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reprimand_MessageLog_ContextId",
                table: "Reprimand");

            migrationBuilder.DropIndex(
                name: "IX_Reprimand_ContextId",
                table: "Reprimand");

            migrationBuilder.DropColumn(
                name: "ContextId",
                table: "Reprimand");
        }
    }
}
