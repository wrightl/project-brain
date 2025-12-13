using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class MoveAvailabilityStatusToCoachProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column to CoachProfiles first
            migrationBuilder.AddColumn<string>(
                name: "AvailabilityStatus",
                table: "CoachProfiles",
                type: "nvarchar(20)",
                nullable: true);

            // Copy data from Users to CoachProfiles
            migrationBuilder.Sql(@"
                UPDATE CoachProfiles
                SET AvailabilityStatus = u.AvailabilityStatus
                FROM CoachProfiles cp
                INNER JOIN Users u ON cp.UserId = u.Id
                WHERE u.AvailabilityStatus IS NOT NULL
            ");

            // Drop column from Users
            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add column back to Users
            migrationBuilder.AddColumn<string>(
                name: "AvailabilityStatus",
                table: "Users",
                type: "nvarchar(20)",
                nullable: true);

            // Copy data back from CoachProfiles to Users
            migrationBuilder.Sql(@"
                UPDATE Users
                SET AvailabilityStatus = cp.AvailabilityStatus
                FROM Users u
                INNER JOIN CoachProfiles cp ON u.Id = cp.UserId
                WHERE cp.AvailabilityStatus IS NOT NULL
            ");

            // Drop column from CoachProfiles
            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "CoachProfiles");
        }
    }
}
