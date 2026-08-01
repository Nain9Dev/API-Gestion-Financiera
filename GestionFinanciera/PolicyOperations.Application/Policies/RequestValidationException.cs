namespace PolicyOperations.Application.Policies;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string message, string code = "validation_failed")
        : base(message)
    {
        Code = code;
    }

    public RequestValidationException(string message, string code, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
