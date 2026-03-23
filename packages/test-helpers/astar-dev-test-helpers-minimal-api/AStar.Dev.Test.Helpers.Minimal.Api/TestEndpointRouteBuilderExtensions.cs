namespace AStar.Dev.Test.Helpers.Minimal.Api;

public static class TestEndpointRouteBuilderExtensions
{
    public static bool ContainsEndpointWithDisplayName(this TestEndpointRouteBuilder builder, string endpointName)
    {
        var endpoints = builder.DataSources.SelectMany(ds => ds.Endpoints);
        var endpoint  = endpoints.FirstOrDefault(d => d.DisplayName?.Contains(endpointName) == true);

        return endpoint != null;
    }

    public static EndpointMetadataCollection GetEndpointMetadata(this TestEndpointRouteBuilder builder, string endpointName)
    {
        var endpoints = builder.DataSources.SelectMany(ds => ds.Endpoints);
        var endpoint  = endpoints.First(d => d.DisplayName?.Contains(endpointName) == true);

        return endpoint.Metadata;
    }

    public static IList<TagsAttribute> GetEndpointMetadataTags(this TestEndpointRouteBuilder builder, string endpointName)
    {
        var endpoints = builder.DataSources.SelectMany(ds => ds.Endpoints);
        var endpoint  = endpoints.First(d => d.DisplayName?.Contains(endpointName) == true);

        return endpoint.Metadata.OfType<TagsAttribute>().ToList();
    }

    public static IList<HttpMethodMetadata> GetEndpointMethodData(this TestEndpointRouteBuilder builder, string endpointName)
    {
        var endpoints = builder.DataSources.SelectMany(ds => ds.Endpoints);
        var endpoint  = endpoints.First(d => d.DisplayName?.Contains(endpointName) == true);

        return endpoint.Metadata.OfType<HttpMethodMetadata>().ToList();
    }

    public static IList<ProducesResponseTypeMetadata> GetEndpointResponseTypes(this TestEndpointRouteBuilder builder, string endpointName)
    {
        var endpoints = builder.DataSources.SelectMany(ds => ds.Endpoints);
        var endpoint  = endpoints.First(d => d.DisplayName?.Contains(endpointName) == true);

        return endpoint.Metadata.OfType<ProducesResponseTypeMetadata>().ToList();
    }

    public static bool ContainsTag(this IList<TagsAttribute> endpoints, string tag)
        => endpoints.Any(endpoint => endpoint.Tags.Contains(tag));

    public static bool ContainsResponseType(this IList<ProducesResponseTypeMetadata> endpoints, string responseType)
        => endpoints.Any(endpoint => endpoint.ContentTypes.Contains(responseType));

    public static bool DefinesResponseTypeWithStatusCode(this IList<ProducesResponseTypeMetadata> endpoints, int statusCode)
        => endpoints.Any(endpoint => endpoint.StatusCode == statusCode);

    public static bool DefinesResponseTypeWithType(this IList<ProducesResponseTypeMetadata> endpoints, string expectedType)
        => endpoints.Any(endpoint => endpoint.Type!.FullName!.Contains(expectedType));

    public static bool IsPost(this IList<HttpMethodMetadata> endpoints)
        => endpoints.Any(endpoint => endpoint.HttpMethods.Contains("POST"));

    public static bool IsGet(this IList<HttpMethodMetadata> endpoints)
        => endpoints.Any(endpoint => endpoint.HttpMethods.Contains("GET"));
}
