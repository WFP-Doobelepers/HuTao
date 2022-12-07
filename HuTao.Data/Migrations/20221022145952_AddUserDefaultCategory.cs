using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddUserDefaultCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultCategoryId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DefaultCategoryId",
                table: "Users",
                column: "DefaultCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ModerationCategory_DefaultCategoryId",
                table: "Users",
                column: "DefaultCategoryId",
                principalTable: "ModerationCategory",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ModerationCategory_DefaultCategoryId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DefaultCategoryId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DefaultCategoryId",
                table: "Users");
        }
    }
}
