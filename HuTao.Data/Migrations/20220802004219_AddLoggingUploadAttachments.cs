using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddLoggingUploadAttachments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UploadAttachments",
                table: "LoggingRules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadAttachments",
                table: "LoggingRules");
        }
    }
}
