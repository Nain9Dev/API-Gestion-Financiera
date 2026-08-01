namespace PolicyOperations.Application.Policies;

public sealed class PolicyPreconditionRequiredException : Exception
{
    public PolicyPreconditionRequiredException()
        : base("The If-Match header is required for this operation.")
    {
    }
}
