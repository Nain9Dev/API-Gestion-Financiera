namespace PolicyOperations.Api.Configuration;

public sealed class PublicDemoOptions
{
    public const string SectionName = "PublicDemo";

    public bool Enabled { get; init; }

    public Guid OrganizationId { get; init; }

    public int RetentionHours { get; init; } = 24;

    public int RequestsPerMinute { get; init; } = 5;
}
