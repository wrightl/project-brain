using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemTagsAndJournalEntrySystemTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntrySystemTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntrySystemTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntrySystemTags_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JournalEntrySystemTags_SystemTags_SystemTagId",
                        column: x => x.SystemTagId,
                        principalTable: "SystemTags",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemTagFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InputType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    FieldOrder = table.Column<int>(type: "int", nullable: false),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Hint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    StepValue = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemTagFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemTagFieldDefinitions_SystemTags_SystemTagId",
                        column: x => x.SystemTagId,
                        principalTable: "SystemTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntrySystemTags_JournalEntryId_SystemTagId",
                table: "JournalEntrySystemTags",
                columns: new[] { "JournalEntryId", "SystemTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntrySystemTags_SystemTagId",
                table: "JournalEntrySystemTags",
                column: "SystemTagId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemTagFieldDefinitions_SystemTagId_FieldKey",
                table: "SystemTagFieldDefinitions",
                columns: new[] { "SystemTagId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemTagFieldDefinitions_SystemTagId_FieldOrder",
                table: "SystemTagFieldDefinitions",
                columns: new[] { "SystemTagId", "FieldOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemTags_Key",
                table: "SystemTags",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemTags_Name",
                table: "SystemTags",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalEntrySystemTags");

            migrationBuilder.DropTable(
                name: "SystemTagFieldDefinitions");

            migrationBuilder.DropTable(
                name: "SystemTags");
        }
    }
}
