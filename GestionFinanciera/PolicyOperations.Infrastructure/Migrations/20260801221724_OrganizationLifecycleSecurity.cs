using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // EF Core migration methods use immutable inline column arrays.

namespace PolicyOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationLifecycleSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Policies])
                BEGIN
                    THROW 51003, 'Policy migration stopped because existing rows require verified organization, currency and activation data.', 1;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "UX_Policies_NormalizedPolicyNumber",
                table: "Policies");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CoverageEndDate",
                table: "Policies",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CoverageStartDate",
                table: "Policies",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Policies",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "InsuredPartyReference",
                table: "Policies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Policies",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Policies",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Policies_OrganizationId_Id",
                table: "Policies",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.CreateTable(
                name: "PolicyTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ActorSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyTransitions", x => x.Id);
                    table.CheckConstraint("CK_PolicyTransitions_Status_Valid", "[FromStatus] IN (N'Draft', N'Active') AND [ToStatus] IN (N'Active', N'Cancelled') AND [FromStatus] <> [ToStatus]");
                    table.ForeignKey(
                        name: "FK_PolicyTransitions_Policies_OrganizationId_PolicyId",
                        columns: x => new { x.OrganizationId, x.PolicyId },
                        principalTable: "Policies",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Policies_Organization_NormalizedPolicyNumber",
                table: "Policies",
                columns: new[] { "OrganizationId", "NormalizedPolicyNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Policies_ActivationData_Complete",
                table: "Policies",
                sql: "[Status] <> N'Active' OR ([InsuredPartyReference] IS NOT NULL AND [CoverageStartDate] IS NOT NULL AND [CoverageEndDate] IS NOT NULL AND [CoverageEndDate] >= [CoverageStartDate])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Policies_Currency_Format",
                table: "Policies",
                sql: "LEN([Currency]) = 3 AND [Currency] = UPPER([Currency]) AND [Currency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%'");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyTransitions_Organization_Policy_OccurredAtUtc",
                table: "PolicyTransitions",
                columns: new[] { "OrganizationId", "PolicyId", "OccurredAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [Policies])
                BEGIN
                    THROW 51004, 'Down migration stopped because organization, currency, lifecycle or concurrency data would be lost.', 1;
                END;
                """);

            migrationBuilder.DropTable(
                name: "PolicyTransitions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Policies_OrganizationId_Id",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "UX_Policies_Organization_NormalizedPolicyNumber",
                table: "Policies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Policies_ActivationData_Complete",
                table: "Policies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Policies_Currency_Format",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "CoverageEndDate",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "CoverageStartDate",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "InsuredPartyReference",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Policies");

            migrationBuilder.CreateIndex(
                name: "UX_Policies_NormalizedPolicyNumber",
                table: "Policies",
                column: "NormalizedPolicyNumber",
                unique: true);
        }
    }
}

#pragma warning restore CA1861
