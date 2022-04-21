using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddWarningTriggerCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "ReprimandAction",
                newName: "Count");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Censor",
                newName: "Count");

            migrationBuilder.AddColumn<long>(
                name: "Count",
                table: "Trigger",
                type: "bigint",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Count",
                table: "Trigger");

            migrationBuilder.RenameColumn(
                name: "Count",
                table: "ReprimandAction",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "Count",
                table: "Censor",
                newName: "Amount");
        }
    }
}