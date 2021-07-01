﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Zhongli.Data;

namespace Zhongli.Data.Migrations
{
    [DbContext(typeof(ZhongliContext))]
    [Migration("20210701155021_GuildEntityForeignKeys")]
    partial class GuildEntityForeignKeys
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.7")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.AuthorizationRules", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.ToTable("AuthorizationRules");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.ChannelAuthorization", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal?>("AddedByGuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("AddedById")
                        .HasColumnType("numeric(20,0)");

                    b.Property<Guid?>("AuthorizationRulesId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Scope")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("AuthorizationRulesId");

                    b.HasIndex("GuildId");

                    b.HasIndex("AddedById", "AddedByGuildId");

                    b.ToTable("ChannelAuthorization");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.GuildAuthorization", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal?>("AddedByGuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("AddedById")
                        .HasColumnType("numeric(20,0)");

                    b.Property<Guid?>("AuthorizationRulesId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Scope")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("AuthorizationRulesId");

                    b.HasIndex("GuildId");

                    b.HasIndex("AddedById", "AddedByGuildId");

                    b.ToTable("GuildAuthorization");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.PermissionAuthorization", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal?>("AddedByGuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("AddedById")
                        .HasColumnType("numeric(20,0)");

                    b.Property<Guid?>("AuthorizationRulesId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Permission")
                        .HasColumnType("integer");

                    b.Property<int>("Scope")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("AuthorizationRulesId");

                    b.HasIndex("GuildId");

                    b.HasIndex("AddedById", "AddedByGuildId");

                    b.ToTable("PermissionAuthorization");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.RoleAuthorization", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal?>("AddedByGuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("AddedById")
                        .HasColumnType("numeric(20,0)");

                    b.Property<Guid?>("AuthorizationRulesId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("RoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Scope")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("AuthorizationRulesId");

                    b.HasIndex("GuildId");

                    b.HasIndex("AddedById", "AddedByGuildId");

                    b.ToTable("RoleAuthorization");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.UserAuthorization", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("AddedById")
                        .HasColumnType("numeric(20,0)");

                    b.Property<Guid?>("AuthorizationRulesId")
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Scope")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("AuthorizationRulesId");

                    b.HasIndex("GuildId");

                    b.HasIndex("AddedById", "GuildId");

                    b.HasIndex("UserId", "GuildId");

                    b.ToTable("UserAuthorization");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Discord.GuildEntity", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal?>("MuteRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Discord.GuildUserEntity", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("DiscriminatorValue")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset?>("JoinedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Nickname")
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("WarningCount")
                        .HasColumnType("integer");

                    b.HasKey("Id", "GuildId");

                    b.HasIndex("GuildId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Logging.LoggingRules", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ModerationChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.ToTable("LoggingRules");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.AntiSpamRules", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<TimeSpan?>("DuplicateMessageTime")
                        .HasColumnType("interval");

                    b.Property<int?>("DuplicateTolerance")
                        .HasColumnType("integer");

                    b.Property<long?>("EmojiLimit")
                        .HasColumnType("bigint");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<long?>("MessageLimit")
                        .HasColumnType("bigint");

                    b.Property<TimeSpan?>("MessageSpamTime")
                        .HasColumnType("interval");

                    b.Property<long?>("NewlineLimit")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("AntiSpamRules");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.AutoModerationRules", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("AntiSpamRulesId")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("BanTriggerId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<Guid?>("KickTriggerId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("AntiSpamRulesId");

                    b.HasIndex("BanTriggerId");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.HasIndex("KickTriggerId");

                    b.ToTable("AutoModerationRules");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Reprimands.Ban", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("DeleteDays")
                        .HasColumnType("bigint");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ModeratorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Reason")
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.HasIndex("ModeratorId", "GuildId");

                    b.HasIndex("UserId", "GuildId");

                    b.ToTable("Ban");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Reprimands.Kick", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ModeratorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Reason")
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.HasIndex("ModeratorId", "GuildId");

                    b.HasIndex("UserId", "GuildId");

                    b.ToTable("Kick");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Reprimands.Mute", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset?>("EndedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<TimeSpan?>("Length")
                        .HasColumnType("interval");

                    b.Property<decimal>("ModeratorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Reason")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("StartedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.HasIndex("ModeratorId", "GuildId");

                    b.HasIndex("UserId", "GuildId");

                    b.ToTable("Mute");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Reprimands.Warning", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<long>("Amount")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ModeratorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("Reason")
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.HasIndex("ModeratorId", "GuildId");

                    b.HasIndex("UserId", "GuildId");

                    b.ToTable("Warning");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Triggers.BanTrigger", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<long>("DeleteDays")
                        .HasColumnType("bigint");

                    b.Property<long>("TriggerAt")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("BanTrigger");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Triggers.KickTrigger", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<long>("TriggerAt")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("KickTrigger");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Triggers.MuteTrigger", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("AutoModerationRulesId")
                        .HasColumnType("uuid");

                    b.Property<TimeSpan?>("Length")
                        .HasColumnType("interval");

                    b.Property<long>("TriggerAt")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("AutoModerationRulesId");

                    b.ToTable("MuteTrigger");
                });

            modelBuilder.Entity("Zhongli.Data.Models.VoiceChat.VoiceChatLink", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("OwnerId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("TextChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("VoiceChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<Guid?>("VoiceChatRulesId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.HasIndex("VoiceChatRulesId");

                    b.HasIndex("OwnerId", "GuildId");

                    b.ToTable("VoiceChatLink");
                });

            modelBuilder.Entity("Zhongli.Data.Models.VoiceChat.VoiceChatRules", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("HubVoiceChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<bool>("PurgeEmpty")
                        .HasColumnType("boolean");

                    b.Property<bool>("ShowJoinLeave")
                        .HasColumnType("boolean");

                    b.Property<decimal>("VoiceChannelCategoryId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("VoiceChatCategoryId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.ToTable("VoiceChatRules");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.AuthorizationRules", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithOne("AuthorizationRules")
                        .HasForeignKey("Zhongli.Data.Models.Authorization.AuthorizationRules", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.ChannelAuthorization", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Authorization.AuthorizationRules", null)
                        .WithMany("ChannelAuthorizations")
                        .HasForeignKey("AuthorizationRulesId");

                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "AddedBy")
                        .WithMany()
                        .HasForeignKey("AddedById", "AddedByGuildId");

                    b.Navigation("AddedBy");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.GuildAuthorization", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Authorization.AuthorizationRules", null)
                        .WithMany("GuildAuthorizations")
                        .HasForeignKey("AuthorizationRulesId");

                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "AddedBy")
                        .WithMany()
                        .HasForeignKey("AddedById", "AddedByGuildId");

                    b.Navigation("AddedBy");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.PermissionAuthorization", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Authorization.AuthorizationRules", null)
                        .WithMany("PermissionAuthorizations")
                        .HasForeignKey("AuthorizationRulesId");

                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "AddedBy")
                        .WithMany()
                        .HasForeignKey("AddedById", "AddedByGuildId");

                    b.Navigation("AddedBy");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.RoleAuthorization", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Authorization.AuthorizationRules", null)
                        .WithMany("RoleAuthorizations")
                        .HasForeignKey("AuthorizationRulesId");

                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "AddedBy")
                        .WithMany()
                        .HasForeignKey("AddedById", "AddedByGuildId");

                    b.Navigation("AddedBy");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.UserAuthorization", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Authorization.AuthorizationRules", null)
                        .WithMany("UserAuthorizations")
                        .HasForeignKey("AuthorizationRulesId");

                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "AddedBy")
                        .WithMany()
                        .HasForeignKey("AddedById", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "User")
                        .WithMany()
                        .HasForeignKey("UserId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AddedBy");

                    b.Navigation("Guild");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Discord.GuildUserEntity", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Logging.LoggingRules", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithOne("LoggingRules")
                        .HasForeignKey("Zhongli.Data.Models.Logging.LoggingRules", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.AntiSpamRules", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.AutoModerationRules", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Moderation.AntiSpamRules", "AntiSpamRules")
                        .WithMany()
                        .HasForeignKey("AntiSpamRulesId");

                    b.HasOne("Zhongli.Data.Models.Moderation.Triggers.BanTrigger", "BanTrigger")
                        .WithMany()
                        .HasForeignKey("BanTriggerId");

                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithOne("AutoModerationRules")
                        .HasForeignKey("Zhongli.Data.Models.Moderation.AutoModerationRules", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Moderation.Triggers.KickTrigger", "KickTrigger")
                        .WithMany()
                        .HasForeignKey("KickTriggerId");

                    b.Navigation("AntiSpamRules");

                    b.Navigation("BanTrigger");

                    b.Navigation("Guild");

                    b.Navigation("KickTrigger");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Reprimands.Ban", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "Moderator")
                        .WithMany()
                        .HasForeignKey("ModeratorId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "User")
                        .WithMany("BanHistory")
                        .HasForeignKey("UserId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("Moderator");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Reprimands.Kick", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "Moderator")
                        .WithMany()
                        .HasForeignKey("ModeratorId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "User")
                        .WithMany("KickHistory")
                        .HasForeignKey("UserId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("Moderator");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Reprimands.Mute", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "Moderator")
                        .WithMany()
                        .HasForeignKey("ModeratorId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "User")
                        .WithMany("MuteHistory")
                        .HasForeignKey("UserId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("Moderator");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Reprimands.Warning", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "Moderator")
                        .WithMany()
                        .HasForeignKey("ModeratorId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "User")
                        .WithMany("WarningHistory")
                        .HasForeignKey("UserId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("Moderator");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.Triggers.MuteTrigger", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Moderation.AutoModerationRules", null)
                        .WithMany("MuteTriggers")
                        .HasForeignKey("AutoModerationRulesId");
                });

            modelBuilder.Entity("Zhongli.Data.Models.VoiceChat.VoiceChatLink", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Zhongli.Data.Models.VoiceChat.VoiceChatRules", null)
                        .WithMany("VoiceChats")
                        .HasForeignKey("VoiceChatRulesId");

                    b.HasOne("Zhongli.Data.Models.Discord.GuildUserEntity", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("Zhongli.Data.Models.VoiceChat.VoiceChatRules", b =>
                {
                    b.HasOne("Zhongli.Data.Models.Discord.GuildEntity", "Guild")
                        .WithOne("VoiceChatRules")
                        .HasForeignKey("Zhongli.Data.Models.VoiceChat.VoiceChatRules", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Authorization.AuthorizationRules", b =>
                {
                    b.Navigation("ChannelAuthorizations");

                    b.Navigation("GuildAuthorizations");

                    b.Navigation("PermissionAuthorizations");

                    b.Navigation("RoleAuthorizations");

                    b.Navigation("UserAuthorizations");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Discord.GuildEntity", b =>
                {
                    b.Navigation("AuthorizationRules");

                    b.Navigation("AutoModerationRules");

                    b.Navigation("LoggingRules");

                    b.Navigation("VoiceChatRules");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Discord.GuildUserEntity", b =>
                {
                    b.Navigation("BanHistory");

                    b.Navigation("KickHistory");

                    b.Navigation("MuteHistory");

                    b.Navigation("WarningHistory");
                });

            modelBuilder.Entity("Zhongli.Data.Models.Moderation.AutoModerationRules", b =>
                {
                    b.Navigation("MuteTriggers");
                });

            modelBuilder.Entity("Zhongli.Data.Models.VoiceChat.VoiceChatRules", b =>
                {
                    b.Navigation("VoiceChats");
                });
#pragma warning restore 612, 618
        }
    }
}
