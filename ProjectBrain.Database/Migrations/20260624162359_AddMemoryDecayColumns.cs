using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoryDecayColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "UserFacts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetrievedAt",
                table: "UserFacts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "UserEpisodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetrievedAt",
                table: "UserEpisodes",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "UserFacts");

            migrationBuilder.DropColumn(
                name: "LastRetrievedAt",
                table: "UserFacts");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "UserEpisodes");

            migrationBuilder.DropColumn(
                name: "LastRetrievedAt",
                table: "UserEpisodes");
        }
    }
}
