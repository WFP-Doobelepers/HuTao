using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HuTao.Data.Migrations
{
    public partial class AddMessageLogging : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MessageLogChannelId",
                table: "LoggingRules",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MessageLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MentionedEveryone = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EditedTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MentionedRolesCount = table.Column<int>(type: "integer", nullable: false),
                    MentionedUsersCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ReferencedMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LogDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LogType = table.Column<int>(type: "integer", nullable: false),
                    MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageLog_Guilds_GuildEntityId",
                        column: x => x.GuildEntityId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageLog_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageLog_MessageLog_UpdatedLogId",
                        column: x => x.UpdatedLogId,
                        principalTable: "MessageLog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageLog_Users_UserId_GuildId",
                        columns: x => new { x.UserId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReactionEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: false),
                    IsAnimated = table.Column<bool>(type: "boolean", nullable: true),
                    EmoteId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactionEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attachment",
                columns: table => new
                {
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Filename = table.Column<string>(type: "text", nullable: false),
                    ProxyUrl = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    MessageLogId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachment", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_Attachment_MessageLog_MessageLogId",
                        column: x => x.MessageLogId,
                        principalTable: "MessageLog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReactionLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    IsCategory = table.Column<bool>(type: "boolean", nullable: false),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LogDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LogType = table.Column<int>(type: "integer", nullable: false),
                    MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildEntityId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReactionLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReactionLog_Guilds_GuildEntityId",
                        column: x => x.GuildEntityId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReactionLog_ReactionEntity_EmoteId",
                        column: x => x.EmoteId,
                        principalTable: "ReactionEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReactionLog_Users_UserId_GuildId",
                        columns: x => new { x.UserId, x.GuildId },
                        principalTable: "Users",
                        principalColumns: new[] { "Id", "GuildId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_MessageLogId",
                table: "Attachment",
                column: "MessageLogId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLog_GuildEntityId",
                table: "MessageLog",
                column: "GuildEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLog_GuildId",
                table: "MessageLog",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLog_UpdatedLogId",
                table: "MessageLog",
                column: "UpdatedLogId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLog_UserId_GuildId",
                table: "MessageLog",
                columns: new[] { "UserId", "GuildId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReactionEntity_EmoteId",
                table: "ReactionEntity",
                column: "EmoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReactionLog_EmoteId",
                table: "ReactionLog",
                column: "EmoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionLog_GuildEntityId",
                table: "ReactionLog",
                column: "GuildEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ReactionLog_UserId_GuildId",
                table: "ReactionLog",
                columns: new[] { "UserId", "GuildId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachment");

            migrationBuilder.DropTable(
                name: "ReactionLog");

            migrationBuilder.DropTable(
                name: "MessageLog");

            migrationBuilder.DropTable(
                name: "ReactionEntity");

            migrationBuilder.DropColumn(
                name: "MessageLogChannelId",
                table: "LoggingRules");
        }
    }
}