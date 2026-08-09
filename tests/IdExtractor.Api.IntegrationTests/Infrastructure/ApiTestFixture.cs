using System.Security.Cryptography;
using System.Text;
using IdExtractor.Modules.Documents.Persistence;
using IdExtractor.Modules.Extraction.Ocr;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace IdExtractor.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API host against a throwaway Postgres container (Testcontainers)
/// with the OCR providers swapped for <see cref="FakeOcrProvider"/> — everything
/// else (auth, versioning, rate limiting, MediatR pipeline, EF Core, audit
/// tracking, PDF generation) runs unmodified. Shared across a test collection so
/// the container only starts once per test run.
/// </summary>
public sealed class ApiTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("idextractor")
        .WithUsername("idextractor")
        .WithPassword("idextractor")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private string? _imageStoreRoot;

    public const string ApiKey = "integration-test-key";
    public const string BurstTestApiKey = "burst-test-key";

    public HttpClient CreateClient() => _factory!.CreateClient();

    public IServiceProvider Services => _factory!.Services;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _imageStoreRoot = Directory.CreateTempSubdirectory("idextractor-it-images-").FullName;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DocumentsDb"] = _postgres.GetConnectionString(),
                    ["ImageStore:RootPath"] = _imageStoreRoot,
                    ["ApiKeys:Keys:0:Name"] = "integration-tests",
                    ["ApiKeys:Keys:0:HashedKey"] = Hash(ApiKey),
                    ["ApiKeys:Keys:1:Name"] = "burst-tests",
                    ["ApiKeys:Keys:1:HashedKey"] = Hash(BurstTestApiKey)
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IOcrProvider>();
                services.AddScoped<IOcrProvider, FakeOcrProvider>();
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();

        if (_imageStoreRoot is not null && Directory.Exists(_imageStoreRoot))
        {
            Directory.Delete(_imageStoreRoot, recursive: true);
        }
    }

    private static string Hash(string apiKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
}

[CollectionDefinition(Name)]
public sealed class ApiTestCollection : ICollectionFixture<ApiTestFixture>
{
    public const string Name = "Api integration tests";
}
