using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMemoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemoryPromotionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CandidateType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CandidateContent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryPromotionAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserEpisodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelatedStrategyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObservationCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEpisodes_UserCopingStrategies_RelatedStrategyId",
                        column: x => x.RelatedStrategyId,
                        principalTable: "UserCopingStrategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_UserEpisodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObservationCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFacts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryPromotionAudits_CreatedAt",
                table: "MemoryPromotionAudits",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryPromotionAudits_UserId",
                table: "MemoryPromotionAudits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEpisodes_RelatedStrategyId",
                table: "UserEpisodes",
                column: "RelatedStrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEpisodes_UserId",
                table: "UserEpisodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEpisodes_UserId_ContentHash",
                table: "UserEpisodes",
                columns: new[] { "UserId", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFacts_UserId",
                table: "UserFacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFacts_UserId_ContentHash",
                table: "UserFacts",
                columns: new[] { "UserId", "ContentHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemoryPromotionAudits");

            migrationBuilder.DropTable(
                name: "UserEpisodes");

            migrationBuilder.DropTable(
                name: "UserFacts");
        }
    }
}
