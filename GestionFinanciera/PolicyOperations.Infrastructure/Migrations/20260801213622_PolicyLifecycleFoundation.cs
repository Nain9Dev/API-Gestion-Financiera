using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolicyOperations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PolicyLifecycleFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "Policies",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedPolicyNumber",
                table: "Policies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Policies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Policies]
                SET
                    [NormalizedPolicyNumber] = UPPER(LTRIM(RTRIM([PolicyNumber]))),
                    [Status] = CASE WHEN [IsActive] = 1 THEN N'Active' ELSE N'Cancelled' END,
                    [CreatedAtUtc] = TODATETIMEOFFSET([IssueDate], '+00:00');
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [Policies]
                    WHERE [NormalizedPolicyNumber] IS NULL
                       OR LEN([NormalizedPolicyNumber]) = 0)
                BEGIN
                    THROW 51000, 'Policy migration stopped because a policy number is empty after normalization.', 1;
                END;

                IF EXISTS (
                    SELECT [NormalizedPolicyNumber]
                    FROM [Policies]
                    GROUP BY [NormalizedPolicyNumber]
                    HAVING COUNT_BIG(*) > 1)
                BEGIN
                    THROW 51001, 'Policy migration stopped because normalized policy numbers are duplicated.', 1;
                END;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "Policies",
                type: "datetimeoffset(7)",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedPolicyNumber",
                table: "Policies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Policies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "IssueDate",
                table: "Policies");

            migrationBuilder.CreateIndex(
                name: "UX_Policies_NormalizedPolicyNumber",
                table: "Policies",
                column: "NormalizedPolicyNumber",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Policies_InsuredAmount_Positive",
                table: "Policies",
                sql: "[InsuredAmount] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Policies_Status_Valid",
                table: "Policies",
                sql: "[Status] IN (N'Draft', N'Active', N'Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [Policies]
                    WHERE [Status] = N'Draft')
                BEGIN
                    THROW 51002, 'Down migration stopped because Draft status cannot be represented by the legacy schema.', 1;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "UX_Policies_NormalizedPolicyNumber",
                table: "Policies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Policies_InsuredAmount_Positive",
                table: "Policies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Policies_Status_Valid",
                table: "Policies");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Policies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssueDate",
                table: "Policies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Policies]
                SET
                    [IsActive] = CASE WHEN [Status] = N'Active' THEN 1 ELSE 0 END,
                    [IssueDate] = CAST(SWITCHOFFSET([CreatedAtUtc], '+00:00') AS datetime2);
                """);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Policies",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "IssueDate",
                table: "Policies",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "NormalizedPolicyNumber",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Policies");
        }
    }
}
