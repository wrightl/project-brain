using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class ExtendCoachMessageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "CoachMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "CoachMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "CoachMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderId",
                table: "CoachMessages",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoiceNoteFileName",
                table: "CoachMessages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoiceNoteUrl",
                table: "CoachMessages",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachMessages_SenderId_CreatedAt",
                table: "CoachMessages",
                columns: new[] { "SenderId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_CoachMessages_Users_SenderId",
                table: "CoachMessages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoachMessages_Users_SenderId",
                table: "CoachMessages");

            migrationBuilder.DropIndex(
                name: "IX_CoachMessages_SenderId_CreatedAt",
                table: "CoachMessages");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "CoachMessages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "CoachMessages");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "CoachMessages");

            migrationBuilder.DropColumn(
                name: "SenderId",
                table: "CoachMessages");

            migrationBuilder.DropColumn(
                name: "VoiceNoteFileName",
                table: "CoachMessages");

            migrationBuilder.DropColumn(
                name: "VoiceNoteUrl",
                table: "CoachMessages");
        }
    }
}
