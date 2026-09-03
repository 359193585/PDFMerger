using System.Net;
using PDFMerger.Services;

namespace PDFMerger.Tests.Services;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("2.0.0", "1.0.0", 1)]
    [InlineData("1.0.0", "2.0.0", -1)]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.10.0", "1.9.0", 1)]
    [InlineData("1.9.0", "1.10.0", -1)]
    [InlineData("2.0.0", "1.99.99", 1)]
    [InlineData("1.99.99", "2.0.0", -1)]
    public void CompareVersions_ReturnsExpectedResult(
        string latest,
        string current,
        int expected)
    {
        var result = UpdateService.CompareVersions(latest, current);

        Assert.Equal(expected, result);
    }


    [Fact]
    public void GetCurrentVersion_ReturnsThreePartVersion()
    {
        var version = UpdateService.GetCurrentVersion();

        Assert.Matches(@"^\d+\.\d+\.\d+$", version);
    }

    #region GetLatestVersionAsync

    [Theory]
    [InlineData("v1.2.3")]
    [InlineData("1.2.3")]
    public async Task GetLatestVersionAsync_WithValidTag_ReturnsVersion(string tag)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{"tag_name":"{{tag}}"}""")
        };

        using var handler = new FakeHttpMessageHandler(response);
        using var client = new HttpClient(handler);

        var result = await UpdateService.GetLatestVersionAsync(client);

        Assert.Equal("1.2.3", result);
    }


    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetLatestVersionAsync_WhenRequestFails_ReturnsNull(
        HttpStatusCode statusCode)
    {
        var response = new HttpResponseMessage(statusCode);

        using var handler = new FakeHttpMessageHandler(response);
        using var client = new HttpClient(handler);

        var result = await UpdateService.GetLatestVersionAsync(client);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WhenTagNameIsMissing_ReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"name":"Release 1.2.3"}""")
        };

        using var handler = new FakeHttpMessageHandler(response);
        using var client = new HttpClient(handler);

        var result = await UpdateService.GetLatestVersionAsync(client);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WhenTagNameIsNull_ReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"tag_name":null}""")
        };

        using var handler = new FakeHttpMessageHandler(response);
        using var client = new HttpClient(handler);

        var result = await UpdateService.GetLatestVersionAsync(client);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0.1")]
    [InlineData("abc")]
    [InlineData("")]
    public async Task GetLatestVersionAsync_WithInvalidVersion_ReturnsNull(
        string tag)
    {
        var json = $$"""{"tag_name":"{{tag}}"}""";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        using var handler = new FakeHttpMessageHandler(response);
        using var client = new HttpClient(handler);

        var result = await UpdateService.GetLatestVersionAsync(client);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WithInvalidJson_ReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json")
        };

        using var handler = new FakeHttpMessageHandler(response);
        using var client = new HttpClient(handler);

        var result = await UpdateService.GetLatestVersionAsync(client);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestVersionAsync_WhenRequestThrows_ReturnsNull()
    {
        using var handler = new ThrowingHttpMessageHandler();
        using var client = new HttpClient(handler);

        var result = await UpdateService.GetLatestVersionAsync(client);

        Assert.Null(result);
    }

    #endregion

    #region Test Helpers FakeHttpMessageHandler

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Simulated network failure.");
        }
    }

    #endregion
}
