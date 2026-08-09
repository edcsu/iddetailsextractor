using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IdExtractor.Api.IntegrationTests.Infrastructure;
using IdExtractor.Modules.Documents.Persistence;
using IdExtractor.SharedKernel.Contracts.Extraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdExtractor.Api.IntegrationTests.Extraction;

[Collection(ApiTestCollection.Name)]
public class ExtractionFlowTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task Posting_without_an_api_key_is_rejected()
    {
        using var client = fixture.CreateClient();
        using var content = BuildUploadContent();

        var response = await client.PostAsync("/api/v1/extractions", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Full_pipeline_extracts_persists_and_serves_the_result()
    {
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFixture.ApiKey);

        using var uploadContent = BuildUploadContent();
        var postResponse = await client.PostAsync("/api/v1/extractions", uploadContent);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        var postBody = await postResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = postBody.GetProperty("id").GetGuid();

        Assert.Equal(nameof(DocumentType.Passport), postBody.GetProperty("documentType").GetString());
        Assert.True(postBody.GetProperty("mrz").GetProperty("valid").GetBoolean());
        Assert.Equal(
            "ANNA MARIA ERIKSSON",
            postBody.GetProperty("fields").GetProperty("fullName").GetProperty("value").GetString());

        var getResponse = await client.GetAsync($"/api/v1/extractions/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, getBody.GetProperty("id").GetGuid());

        var listResponse = await client.GetAsync("/api/v1/extractions?pageNumber=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(listBody.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);

        var reportResponse = await client.GetAsync($"/api/v1/extractions/{id}/report.pdf");
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        var reportBytes = await reportResponse.Content.ReadAsByteArrayAsync();
        Assert.True(reportBytes.Length > 4);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(reportBytes, 0, 4));
    }

    [Fact]
    public async Task Getting_an_unknown_id_returns_not_found()
    {
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFixture.ApiKey);

        var response = await client.GetAsync($"/api/v1/extractions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Extraction_writes_an_audit_log_row_without_leaking_pii()
    {
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFixture.ApiKey);

        using var uploadContent = BuildUploadContent();
        var postResponse = await client.PostAsync("/api/v1/extractions", uploadContent);
        var postBody = await postResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = postBody.GetProperty("id").GetGuid();

        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
        var auditRow = await db.AuditLogs.FirstOrDefaultAsync(a => a.EntityId == id.ToString());

        Assert.NotNull(auditRow);
        Assert.Equal("ExtractionRecord", auditRow.EntityName);
        Assert.Equal("Added", auditRow.Action);
        Assert.DoesNotContain("ANNA MARIA ERIKSSON", auditRow.ChangesJson ?? "");
    }

    private static MultipartFormDataContent BuildUploadContent()
    {
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(TestImages.CreateValidPng());
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(imageContent, "file", "document.png");
        return content;
    }
}
