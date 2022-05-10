using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class LinkedCommandAuthorizationGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Criterion_LinkedCommand_LinkedCommandId",
                table: "Criterion");

            migrationBuilder.DropIndex(
                name: "IX_Criterion_LinkedCommandId",
                table: "Criterion");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "LinkedCommand");

            migrationBuilder.DropColumn(
                name: "LinkedCommandId",
                table: "Criterion");

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedCommandId",
                table: "AuthorizationGroup",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationGroup_LinkedCommandId",
                table: "AuthorizationGroup",
                column: "LinkedCommandId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationGroup_LinkedCommand_LinkedCommandId",
                table: "AuthorizationGroup",
                column: "LinkedCommandId",
                principalTable: "LinkedCommand",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationGroup_LinkedCommand_LinkedCommandId",
                table: "AuthorizationGroup");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationGroup_LinkedCommandId",
                table: "AuthorizationGroup");

            migrationBuilder.DropColumn(
                name: "LinkedCommandId",
                table: "AuthorizationGroup");

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "LinkedCommand",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedCommandId",
                table: "Criterion",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Criterion_LinkedCommandId",
                table: "Criterion",
                column: "LinkedCommandId");

            migrationBuilder.AddForeignKey(
                name: "FK_Criterion_LinkedCommand_LinkedCommandId",
                table: "Criterion",
                column: "LinkedCommandId",
                principalTable: "LinkedCommand",
                principalColumn: "Id");
        }
    }
}
