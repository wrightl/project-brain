using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCopingStrategyCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCopingStrategies_CopingStrategies_CopingStrategyId",
                table: "UserCopingStrategies");

            migrationBuilder.DropTable(
                name: "CopingStrategies");

            migrationBuilder.DropIndex(
                name: "IX_UserCopingStrategies_CopingStrategyId",
                table: "UserCopingStrategies");

            migrationBuilder.DropIndex(
                name: "IX_UserCopingStrategies_UserId_CopingStrategyId",
                table: "UserCopingStrategies");

            migrationBuilder.DropColumn(
                name: "CopingStrategyId",
                table: "UserCopingStrategies");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "UserCopingStrategies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "UserCopingStrategies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "UserCopingStrategies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "UserCopingStrategies");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "UserCopingStrategies");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "UserCopingStrategies");

            migrationBuilder.AddColumn<Guid>(
                name: "CopingStrategyId",
                table: "UserCopingStrategies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CopingStrategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopingStrategies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCopingStrategies_CopingStrategyId",
                table: "UserCopingStrategies",
                column: "CopingStrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCopingStrategies_UserId_CopingStrategyId",
                table: "UserCopingStrategies",
                columns: new[] { "UserId", "CopingStrategyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCopingStrategies_CopingStrategies_CopingStrategyId",
                table: "UserCopingStrategies",
                column: "CopingStrategyId",
                principalTable: "CopingStrategies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
