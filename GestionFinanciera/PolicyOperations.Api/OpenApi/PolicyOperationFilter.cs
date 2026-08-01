using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PolicyOperations.Api.OpenApi;

public sealed class PolicyOperationFilter : IOperationFilter
{
    private const string EtagExample = "\"AAAAAAAAAAE=\"";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        DocumentIfMatch(operation);
        DocumentResponseContentTypes(operation);
    }

    private static void DocumentIfMatch(OpenApiOperation operation)
    {
        var ifMatch = operation.Parameters?.SingleOrDefault(parameter =>
            string.Equals(parameter.Name, "If-Match", StringComparison.OrdinalIgnoreCase));

        if (ifMatch is not OpenApiParameter parameter)
        {
            return;
        }

        parameter.Required = true;
        parameter.Description =
            "Paste the complete strong ETag, including quotation marks. " +
            $"Example: {EtagExample}";
        parameter.Example = JsonValue.Create(EtagExample);
    }

    private static void DocumentResponseContentTypes(OpenApiOperation operation)
    {
        foreach (var (statusCode, response) in operation.Responses ?? [])
        {
            if (response.Content is null || response.Content.Count == 0)
            {
                continue;
            }

            var mediaType = response.Content.Values.First();
            response.Content.Clear();
            response.Content.Add(
                statusCode.StartsWith('2')
                    ? "application/json"
                    : "application/problem+json",
                mediaType);
        }
    }
}
