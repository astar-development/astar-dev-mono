using System.Text.Json;
using Microsoft.Playwright;

namespace AStarDev.WallpaperScraper.TestsUnit.Services;

internal sealed class MockResponse : IResponse
{
    public int Status { get; set; }
    public Func<Task<string?>> TextAsyncFunc { get; set; } = () => Task.FromResult<string?>(null);

    public string Url => throw new NotImplementedException();
    public string StatusText {get;set;} = string.Empty;
    public bool Ok => throw new NotImplementedException();
    public string Headers => throw new NotImplementedException();

    public IFrame Frame => throw new NotImplementedException();

    public bool FromServiceWorker => throw new NotImplementedException();

    Dictionary<string, string> IResponse.Headers => throw new NotImplementedException();

    public IRequest Request => throw new NotImplementedException();

    public Task<JsonElement> JsonAsync() => throw new NotImplementedException();
    public Task<byte[]> BodyAsync() => throw new NotImplementedException();

    public Task<Dictionary<string, string>> AllHeadersAsync()
    {
        throw new NotImplementedException();
    }

    public Task<string?> FinishedAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Header>> HeadersArrayAsync()
    {
        throw new NotImplementedException();
    }

    public Task<string?> HeaderValueAsync(string name)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<string>> HeaderValuesAsync(string name)
    {
        throw new NotImplementedException();
    }

    public Task<string> HttpVersionAsync()
    {
        throw new NotImplementedException();
    }

    Task<JsonElement?> IResponse.JsonAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ResponseSecurityDetailsResult?> SecurityDetailsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ResponseServerAddrResult?> ServerAddrAsync()
    {
        throw new NotImplementedException();
    }

    public Task<T> JsonAsync<T>()
    {
        throw new NotImplementedException();
    }

    public Task<string> TextAsync()
    {
        throw new NotImplementedException();
    }
}
