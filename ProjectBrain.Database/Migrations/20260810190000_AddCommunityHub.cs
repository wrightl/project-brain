using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260810190000_AddCommunityHub")]
    public class AddCommunityHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityChannels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunityPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false),
                    HiddenByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    HiddenAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityPosts_CommunityChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "CommunityChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunityPosts_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityReactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityReactions_CommunityPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunityReactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommunityReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityReports_CommunityPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunityReports_Users_ReporterUserId",
                        column: x => x.ReporterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityChannels_IsActive_SortOrder",
                table: "CommunityChannels",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityChannels_Slug",
                table: "CommunityChannels",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_AuthorUserId",
                table: "CommunityPosts",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_ChannelId_CreatedAt",
                table: "CommunityPosts",
                columns: new[] { "ChannelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReactions_PostId_UserId",
                table: "CommunityReactions",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReactions_UserId",
                table: "CommunityReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_PostId",
                table: "CommunityReports",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_ReporterUserId",
                table: "CommunityReports",
                column: "ReporterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_Status_CreatedAt",
                table: "CommunityReports",
                columns: new[] { "Status", "CreatedAt" });

            // Use raw SQL — InsertData requires entity mapping in the migration target model/Designer.
            migrationBuilder.Sql("""
                INSERT INTO [CommunityChannels] ([Id], [Slug], [Name], [Description], [SortOrder], [IsActive], [CreatedAt], [UpdatedAt])
                VALUES
                ('11111111-1111-1111-1111-111111111101', 'introductions', 'Introductions', 'Say hello and share a bit about yourself.', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                ('11111111-1111-1111-1111-111111111102', 'wins', 'Wins', 'Celebrate small and big wins.', 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                ('11111111-1111-1111-1111-111111111103', 'struggles', N'Struggles', N'Share what''s hard — you''re not alone.', 3, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
                ('11111111-1111-1111-1111-111111111104', 'tips', 'Tips', 'Practical tips and strategies that help.', 4, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CommunityReactions");
            migrationBuilder.DropTable(name: "CommunityReports");
            migrationBuilder.DropTable(name: "CommunityPosts");
            migrationBuilder.DropTable(name: "CommunityChannels");
        }
    }
}
