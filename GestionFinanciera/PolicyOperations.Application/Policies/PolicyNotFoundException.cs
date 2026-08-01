namespace PolicyOperations.Application.Policies;

public sealed class PolicyNotFoundException : Exception
{
    public PolicyNotFoundException()
        : base("Policy was not found.")
    {
    }
}
