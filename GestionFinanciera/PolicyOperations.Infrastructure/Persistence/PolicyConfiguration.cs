using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Infrastructure.Persistence;

internal sealed class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable(
            "Policies",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Policies_ActivationData_Complete",
                    "[Status] <> N'Active' OR " +
                    "([InsuredPartyReference] IS NOT NULL AND " +
                    "[CoverageStartDate] IS NOT NULL AND " +
                    "[CoverageEndDate] IS NOT NULL AND " +
                    "[CoverageEndDate] >= [CoverageStartDate])");
                tableBuilder.HasCheckConstraint(
                    "CK_Policies_Currency_Format",
                    "LEN([Currency]) = 3 AND " +
                    "[Currency] = UPPER([Currency]) AND " +
                    "[Currency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%'");
                tableBuilder.HasCheckConstraint(
                    "CK_Policies_InsuredAmount_Positive",
                    "[InsuredAmount] > 0");
                tableBuilder.HasCheckConstraint(
                    "CK_Policies_Status_Valid",
                    "[Status] IN (N'Draft', N'Active', N'Cancelled')");
            });

        builder.HasKey(policy => policy.Id);
        builder.HasAlternateKey(policy => new { policy.OrganizationId, policy.Id })
            .HasName("AK_Policies_OrganizationId_Id");

        builder.Property(policy => policy.OrganizationId)
            .IsRequired();

        builder.Property(policy => policy.PolicyNumber)
            .IsRequired()
            .HasMaxLength(Policy.MaxPolicyNumberLength);

        builder.Property(policy => policy.NormalizedPolicyNumber)
            .IsRequired()
            .HasMaxLength(Policy.MaxPolicyNumberLength);

        builder.HasIndex(policy => new
        {
            policy.OrganizationId,
            policy.NormalizedPolicyNumber
        })
            .IsUnique()
            .HasDatabaseName("UX_Policies_Organization_NormalizedPolicyNumber");

        builder.Property(policy => policy.InsuredAmount)
            .HasPrecision(18, 2);

        builder.Property(policy => policy.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.Property(policy => policy.InsuredPartyReference)
            .HasMaxLength(Policy.MaxInsuredPartyReferenceLength);

        builder.Property(policy => policy.CoverageStartDate)
            .HasColumnType("date");

        builder.Property(policy => policy.CoverageEndDate)
            .HasColumnType("date");

        builder.Property(policy => policy.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(policy => policy.CreatedAtUtc)
            .HasColumnType("datetimeoffset(7)");

        builder.Property(policy => policy.Version)
            .IsRowVersion();
    }
}
