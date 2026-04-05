using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net;
using System.Text;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Fakes;

internal static class FakeGraphClients
{
    internal static GraphServiceClient CreateThrowing(Exception exceptionToThrow)
    {
        ThrowingHttpMessageHandler? handler = null;
        HttpClient? httpClient = null;
        HttpClientRequestAdapter? adapter = null;
        try
        {
            handler    = new ThrowingHttpMessageHandler(exceptionToThrow);
            httpClient = new HttpClient(handler);
            handler    = null;
            adapter    = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
            var client = new GraphServiceClient(adapter);
            httpClient = null;
            adapter    = null;

            return client;
        }
        finally
        {
            adapter?.Dispose();
            httpClient?.Dispose();
            handler?.Dispose();
        }
    }

    internal static (GraphServiceClient Client, CapturingHttpMessageHandler Handler) CreateCapturing()
    {
        var capturingHandler = new CapturingHttpMessageHandler();
        HttpClient? httpClient = null;
        HttpClientRequestAdapter? adapter = null;
        try
        {
            httpClient = new HttpClient(capturingHandler);
            adapter    = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
            var client = new GraphServiceClient(adapter);
            httpClient = null;
            adapter    = null;

            return (client, capturingHandler);
        }
        finally
        {
            adapter?.Dispose();
            httpClient?.Dispose();
        }
    }

    private sealed class ThrowingHttpMessageHandler(Exception exceptionToThrow) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromException<HttpResponseMessage>(exceptionToThrow);
    }
}

internal sealed class CapturingHttpMessageHandler : HttpMessageHandler
{
    public List<string> CapturedPaths { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        CapturedPaths.Add(path);

        var json = path switch
        {
            var p when p.EndsWith("/me/drive", StringComparison.OrdinalIgnoreCase) =>
                """{"id":"test-drive-id"}""",
            var p when p.EndsWith("/content", StringComparison.OrdinalIgnoreCase) =>
                """{"id":"test-item-id","name":"uploaded.txt"}""",
            _ => "{}"
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}
