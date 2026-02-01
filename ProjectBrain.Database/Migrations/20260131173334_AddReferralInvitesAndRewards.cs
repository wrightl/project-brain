using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projectbrain.database.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralInvitesAndRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferralInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InviterUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RecipientEmailNormalized = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResendCount = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedByUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RewardedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralInvites_Users_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReferralInvites_Users_InviterUserId",
                        column: x => x.InviterUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReferralRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferralInviteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeneficiaryUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Months = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedToStripeSubscriptionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TriggeringStripeInvoiceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralRewards_ReferralInvites_ReferralInviteId",
                        column: x => x.ReferralInviteId,
                        principalTable: "ReferralInvites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReferralRewards_Users_BeneficiaryUserId",
                        column: x => x.BeneficiaryUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferralInvites_AcceptedByUserId",
                table: "ReferralInvites",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralInvites_ExpiresAt",
                table: "ReferralInvites",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralInvites_InviterUserId_RecipientEmailNormalized",
                table: "ReferralInvites",
                columns: new[] { "InviterUserId", "RecipientEmailNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferralInvites_Status",
                table: "ReferralInvites",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralInvites_TokenHash",
                table: "ReferralInvites",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferralRewards_BeneficiaryUserId",
                table: "ReferralRewards",
                column: "BeneficiaryUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralRewards_ReferralInviteId_BeneficiaryUserId",
                table: "ReferralRewards",
                columns: new[] { "ReferralInviteId", "BeneficiaryUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferralRewards_Status",
                table: "ReferralRewards",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferralRewards");

            migrationBuilder.DropTable(
                name: "ReferralInvites");
        }
    }
}
