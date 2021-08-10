using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class CensoredReprimandNavigation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Options",
                table: "ReprimandAction");

            migrationBuilder.DropColumn(
                name: "Pattern",
                table: "ReprimandAction");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
