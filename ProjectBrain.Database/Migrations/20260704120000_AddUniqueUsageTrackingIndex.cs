using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueUsageTrackingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsageTrackings_UserId_UsageType_PeriodType_PeriodStart",
                table: "UsageTrackings");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTrackings_UserId_UsageType_PeriodType_PeriodStart",
                table: "UsageTrackings",
                columns: new[] { "UserId", "UsageType", "PeriodType", "PeriodStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsageTrackings_UserId_UsageType_PeriodType_PeriodStart",
                table: "UsageTrackings");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTrackings_UserId_UsageType_PeriodType_PeriodStart",
                table: "UsageTrackings",
                columns: new[] { "UserId", "UsageType", "PeriodType", "PeriodStart" });
        }
    }
}
