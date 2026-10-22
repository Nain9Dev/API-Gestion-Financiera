using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PolicyOperations.Api.OpenApi;

/// <summary>
/// Completes what the native generator cannot infer from the controllers: anonymous endpoints
/// carry no security requirement, <c>If-Match</c> is required and shows a quoted example, and
/// responses are described with the media types the API actually writes.
/// </summary>
public sealed class PolicyOperationTransformer : IOpenApiOperationTransformer
{
    private const string EtagExample = "\"AAAAAAAAAAE=\"";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any())
        {
            operation.Security = [];
        }

        DocumentIfMatch(operation);
        DocumentResponseContentTypes(operation);

        return Task.CompletedTask;
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
