using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolicyOperations.Domain.Policies;

namespace PolicyOperations.Infrastructure.Persistence;

internal sealed class PolicyTransitionConfiguration : IEntityTypeConfiguration<PolicyTransition>
{
    public void Configure(EntityTypeBuilder<PolicyTransition> builder)
    {
        builder.ToTable(
            "PolicyTransitions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_PolicyTransitions_Status_Valid",
                    "[FromStatus] IN (N'Draft', N'Active') AND " +
                    "[ToStatus] IN (N'Active', N'Cancelled') AND " +
                    "[FromStatus] <> [ToStatus]");
            });

        builder.HasKey(transition => transition.Id);

        builder.Property(transition => transition.OrganizationId)
            .IsRequired();

        builder.Property(transition => transition.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(transition => transition.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(transition => transition.OccurredAtUtc)
            .HasColumnType("datetimeoffset(7)");

        builder.Property(transition => transition.ActorSubject)
            .IsRequired()
            .HasMaxLength(PolicyTransition.MaxActorSubjectLength);

        builder.Property(transition => transition.Reason)
            .HasMaxLength(Policy.MaxCancellationReasonLength);

        builder.Property(transition => transition.CorrelationId)
            .IsRequired()
            .HasMaxLength(PolicyTransition.MaxCorrelationIdLength);

        builder.HasOne<Policy>()
            .WithMany()
            .HasForeignKey(transition => new
            {
                transition.OrganizationId,
                transition.PolicyId
            })
            .HasPrincipalKey(policy => new
            {
                policy.OrganizationId,
                policy.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transition => new
        {
            transition.OrganizationId,
            transition.PolicyId,
            transition.OccurredAtUtc,
            transition.Id
        })
            .HasDatabaseName("IX_PolicyTransitions_Organization_Policy_OccurredAtUtc");
    }
}
