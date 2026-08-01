namespace PolicyOperations.Application.Policies;

public sealed class PolicyConcurrencyException : Exception
{
    public PolicyConcurrencyException()
        : base("The policy changed after the supplied version was read.")
    {
    }

    public PolicyConcurrencyException(Exception innerException)
        : base("The policy changed after the supplied version was read.", innerException)
    {
    }
}
