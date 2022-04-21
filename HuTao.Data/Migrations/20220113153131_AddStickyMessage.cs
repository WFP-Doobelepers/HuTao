using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuTao.Data.Migrations
{
    public partial class AddStickyMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MessageTemplateId",
                table: "Embed",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MessageTemplateId",
                table: "Attachment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MessageTemplateId",
                table: "ActionRow",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MessageTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowMentions = table.Column<bool>(type: "boolean", nullable: false),
                    ReplaceTimestamps = table.Column<bool>(type: "boolean", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StickyMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeDelay = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CountDelay = table.Column<long>(type: "bigint", nullable: true),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StickyMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StickyMessage_Guilds_GuildEntityId",
                        column: x => x.GuildEntityId,
                        principalTable: "Guilds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StickyMessage_MessageTemplate_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "MessageTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Embed_MessageTemplateId",
                table: "Embed",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_MessageTemplateId",
                table: "Attachment",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRow_MessageTemplateId",
                table: "ActionRow",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_StickyMessage_GuildEntityId",
                table: "StickyMessage",
                column: "GuildEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_StickyMessage_TemplateId",
                table: "StickyMessage",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionRow_MessageTemplate_MessageTemplateId",
                table: "ActionRow",
                column: "MessageTemplateId",
                principalTable: "MessageTemplate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachment_MessageTemplate_MessageTemplateId",
                table: "Attachment",
                column: "MessageTemplateId",
                principalTable: "MessageTemplate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Embed_MessageTemplate_MessageTemplateId",
                table: "Embed",
                column: "MessageTemplateId",
                principalTable: "MessageTemplate",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionRow_MessageTemplate_MessageTemplateId",
                table: "ActionRow");

            migrationBuilder.DropForeignKey(
                name: "FK_Attachment_MessageTemplate_MessageTemplateId",
                table: "Attachment");

            migrationBuilder.DropForeignKey(
                name: "FK_Embed_MessageTemplate_MessageTemplateId",
                table: "Embed");

            migrationBuilder.DropTable(
                name: "StickyMessage");

            migrationBuilder.DropTable(
                name: "MessageTemplate");

            migrationBuilder.DropIndex(
                name: "IX_Embed_MessageTemplateId",
                table: "Embed");

            migrationBuilder.DropIndex(
                name: "IX_Attachment_MessageTemplateId",
                table: "Attachment");

            migrationBuilder.DropIndex(
                name: "IX_ActionRow_MessageTemplateId",
                table: "ActionRow");

            migrationBuilder.DropColumn(
                name: "MessageTemplateId",
                table: "Embed");

            migrationBuilder.DropColumn(
                name: "MessageTemplateId",
                table: "Attachment");

            migrationBuilder.DropColumn(
                name: "MessageTemplateId",
                table: "ActionRow");
        }
    }
}