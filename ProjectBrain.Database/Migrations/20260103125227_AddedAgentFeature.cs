using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class AddedAgentFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToolName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToolParameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToolResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentActions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToolExecutionHistory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    TotalSteps = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentWorkflows_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentActions_UserId",
                table: "AgentActions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkflows_UserId",
                table: "AgentWorkflows",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentActions");

            migrationBuilder.DropTable(
                name: "AgentWorkflows");
        }
    }
}
