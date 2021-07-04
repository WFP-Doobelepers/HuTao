using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class AuthorizationGrouping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AuthorizationGroupId",
                table: "AuthorizationRule",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCategory",
                table: "AuthorizationRule",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRule_AuthorizationGroupId",
                table: "AuthorizationRule",
                column: "AuthorizationGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorizationRule_AuthorizationRule_AuthorizationGroupId",
                table: "AuthorizationRule",
                column: "AuthorizationGroupId",
                principalTable: "AuthorizationRule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorizationRule_AuthorizationRule_AuthorizationGroupId",
                table: "AuthorizationRule");

            migrationBuilder.DropIndex(
                name: "IX_AuthorizationRule_AuthorizationGroupId",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "AuthorizationGroupId",
                table: "AuthorizationRule");

            migrationBuilder.DropColumn(
                name: "IsCategory",
                table: "AuthorizationRule");
        }
    }
}
