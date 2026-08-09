using System.Net;
using IdExtractor.Api.IntegrationTests.Infrastructure;

namespace IdExtractor.Api.IntegrationTests.Extraction;

[Collection(ApiTestCollection.Name)]
public class RateLimitingTests(ApiTestFixture fixture)
{
    // Uses a dedicated API key so deliberately exhausting its budget doesn't affect
    // the fixed 60-req/min window other tests in this collection share.
    [Fact]
    public async Task Exceeding_the_per_key_limit_returns_429()
    {
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFixture.BurstTestApiKey);

        HttpStatusCode? rateLimited = null;
        for (var i = 0; i < 65 && rateLimited is null; i++)
        {
            var response = await client.GetAsync("/api/v1/extractions?pageSize=1");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rateLimited = response.StatusCode;
            }
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimited);
    }
}
