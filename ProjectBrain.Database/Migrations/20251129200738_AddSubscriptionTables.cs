using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoachMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CoachId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachMessages_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CoachMessages_Users_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CoachMessages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "ExternalIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IntegrationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalIntegrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileStorageUsages",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TotalBytes = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileStorageUsages", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_FileStorageUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResearchReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionExclusions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExcludedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExcludedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionExclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionExclusions_Users_ExcludedBy",
                        column: x => x.ExcludedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubscriptionExclusions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnableUserSubscriptions = table.Column<bool>(type: "bit", nullable: false),
                    EnableCoachSubscriptions = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionSettings_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Features = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageTrackings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UsageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeriodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageTrackings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageTrackings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TierId = table.Column<int>(type: "int", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StripePriceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CanceledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionTiers_TierId",
                        column: x => x.TierId,
                        principalTable: "SubscriptionTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachMessages_CoachId",
                table: "CoachMessages",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachMessages_ConnectionId",
                table: "CoachMessages",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachMessages_UserId_CoachId_CreatedAt",
                table: "CoachMessages",
                columns: new[] { "UserId", "CoachId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIntegrations_UserId_IntegrationType",
                table: "ExternalIntegrations",
                columns: new[] { "UserId", "IntegrationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchReports_UserId",
                table: "ResearchReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionExclusions_ExcludedBy",
                table: "SubscriptionExclusions",
                column: "ExcludedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionExclusions_UserId_UserType",
                table: "SubscriptionExclusions",
                columns: new[] { "UserId", "UserType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionSettings_UpdatedBy",
                table: "SubscriptionSettings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTrackings_UserId_UsageType_PeriodType_PeriodStart",
                table: "UsageTrackings",
                columns: new[] { "UserId", "UsageType", "PeriodType", "PeriodStart" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_Status_UserType",
                table: "UserSubscriptions",
                columns: new[] { "Status", "UserType" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_TierId",
                table: "UserSubscriptions",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_UserType",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "UserType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachMessages");

            migrationBuilder.DropTable(
                name: "ExternalIntegrations");

            migrationBuilder.DropTable(
                name: "FileStorageUsages");

            migrationBuilder.DropTable(
                name: "ResearchReports");

            migrationBuilder.DropTable(
                name: "SubscriptionExclusions");

            migrationBuilder.DropTable(
                name: "SubscriptionSettings");

            migrationBuilder.DropTable(
                name: "UsageTrackings");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionTiers");
        }
    }
}
