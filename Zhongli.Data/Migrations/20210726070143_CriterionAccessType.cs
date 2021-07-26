using Microsoft.EntityFrameworkCore.Migrations;

namespace Zhongli.Data.Migrations
{
    public partial class CriterionAccessType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChannelTimeTracking_ChannelId",
                table: "TimeTrackingRules",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Access",
                table: "AuthorizationGroup",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChannelTimeTracking_ChannelId",
                table: "TimeTrackingRules");

            migrationBuilder.DropColumn(
                name: "Access",
                table: "AuthorizationGroup");
        }
    }
}
