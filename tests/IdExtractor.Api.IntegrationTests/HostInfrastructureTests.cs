using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdExtractor.Api.IntegrationTests;

public class HostInfrastructureTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // "Testing" skips the startup database migration (see Program.cs) — these tests
    // only touch endpoints that don't need Postgres at all.
    private HttpClient CreateClient() =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();

    [Fact]
    public async Task Health_endpoint_is_reachable_without_an_api_key()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task V1_openapi_document_is_published_with_the_expected_title()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ID Details Extractor API", body);
    }
}
