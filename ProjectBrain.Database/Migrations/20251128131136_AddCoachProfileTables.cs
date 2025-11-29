using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachProfileTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoachProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachAgeGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachProfileId = table.Column<int>(type: "int", nullable: false),
                    AgeGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachAgeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachAgeGroups_CoachProfiles_CoachProfileId",
                        column: x => x.CoachProfileId,
                        principalTable: "CoachProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachQualifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachProfileId = table.Column<int>(type: "int", nullable: false),
                    Qualification = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachQualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachQualifications_CoachProfiles_CoachProfileId",
                        column: x => x.CoachProfileId,
                        principalTable: "CoachProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachSpecialisms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoachProfileId = table.Column<int>(type: "int", nullable: false),
                    Specialism = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachSpecialisms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachSpecialisms_CoachProfiles_CoachProfileId",
                        column: x => x.CoachProfileId,
                        principalTable: "CoachProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachAgeGroups_CoachProfileId",
                table: "CoachAgeGroups",
                column: "CoachProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachProfiles_UserId",
                table: "CoachProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachQualifications_CoachProfileId",
                table: "CoachQualifications",
                column: "CoachProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachSpecialisms_CoachProfileId",
                table: "CoachSpecialisms",
                column: "CoachProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachAgeGroups");

            migrationBuilder.DropTable(
                name: "CoachQualifications");

            migrationBuilder.DropTable(
                name: "CoachSpecialisms");

            migrationBuilder.DropTable(
                name: "CoachProfiles");
        }
    }
}
