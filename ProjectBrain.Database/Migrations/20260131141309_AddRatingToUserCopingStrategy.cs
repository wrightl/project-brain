using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingToUserCopingStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "UserCopingStrategies",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "UserCopingStrategies");
        }
    }
}
