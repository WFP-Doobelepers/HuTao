using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddLoggingOptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Verbose",
                table: "LoggingRules");

            migrationBuilder.AddColumn<int>(
                name: "Options",
                table: "LoggingRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Options",
                table: "LoggingRules");

            migrationBuilder.AddColumn<bool>(
                name: "Verbose",
                table: "LoggingRules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}