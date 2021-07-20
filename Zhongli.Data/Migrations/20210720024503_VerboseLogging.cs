using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class VerboseLogging : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Verbose",
                table: "LoggingRules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Verbose",
                table: "LoggingRules");
        }
    }
}
