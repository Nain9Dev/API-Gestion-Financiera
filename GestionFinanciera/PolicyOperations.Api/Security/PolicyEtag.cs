using PolicyOperations.Application.Policies;

namespace PolicyOperations.Api.Security;

public static class PolicyEtag
{
    public static byte[] ParseRequired(string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            throw new PolicyPreconditionRequiredException();
        }

        var value = ifMatch.Trim();

        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase) ||
            value.Length < 3 ||
            value[0] != '"' ||
            value[^1] != '"' ||
            value.Contains(','))
        {
            throw CreateInvalidEtagException();
        }

        try
        {
            var version = Convert.FromBase64String(value[1..^1]);

            if (version.Length != PolicyService.SqlServerRowVersionLength)
            {
                throw CreateInvalidEtagException();
            }

            return version;
        }
        catch (FormatException exception)
        {
            throw new RequestValidationException(
                "If-Match must contain one strong base64 ETag.",
                "etag_invalid",
                exception);
        }
    }

    public static string Format(string version)
    {
        return $"\"{version}\"";
    }

    private static RequestValidationException CreateInvalidEtagException()
    {
        return new RequestValidationException(
            "If-Match must contain one strong base64 ETag.",
            "etag_invalid");
    }
}
