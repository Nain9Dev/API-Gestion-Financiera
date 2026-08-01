namespace PolicyOperations.Application.Policies;

public sealed class PolicyNumberConflictException : Exception
{
    public PolicyNumberConflictException()
        : base("A policy with the same normalized policy number already exists.")
    {
    }

    public PolicyNumberConflictException(Exception innerException)
        : base("A policy with the same normalized policy number already exists.", innerException)
    {
    }
}
