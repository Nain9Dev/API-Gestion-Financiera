namespace PolicyOperations.Api.Contracts.Policies;

public sealed class CreatePolicyRequest
{
    public string? PolicyNumber { get; init; }

    public decimal InsuredAmount { get; init; }

    public string? Currency { get; init; }
}
