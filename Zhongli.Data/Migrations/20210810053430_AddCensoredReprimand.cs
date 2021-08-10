using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class AddCensoredReprimand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "ReprimandAction",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Options",
                table: "ReprimandAction",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pattern",
                table: "ReprimandAction",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "Pattern",
                table: "ReprimandAction");
        }
    }
}
