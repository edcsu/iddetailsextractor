using IdExtractor.SharedKernel.Auditing;
using IdExtractor.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdExtractor.Modules.Extraction.Tests.SharedKernel;

public class AuditSaveChangesInterceptorTests
{
    private sealed class TestActor(string? actorId) : ICurrentActor
    {
        public string? ActorId { get; } = actorId;
    }

    private sealed class Widget : AuditableEntity
    {
        public string Name { get; set; } = "";

        [Sensitive]
        public string? DateOfBirth { get; set; }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options, ICurrentActor actor)
        : DbContext(options)
    {
        public DbSet<Widget> Widgets => Set<Widget>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor(actor));
    }

    private static TestDbContext CreateContext(string? actorId = "api-key-1")
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options, new TestActor(actorId));
    }

    [Fact]
    public void Entity_Id_is_a_version7_guid()
    {
        var widget = new Widget();

        Assert.Equal(7, widget.Id.Version);
    }

    [Fact]
    public async Task Adding_an_entity_stamps_created_audit_fields()
    {
        await using var db = CreateContext(actorId: "actor-42");
        var widget = new Widget { Name = "Widget A" };
        db.Widgets.Add(widget);

        await db.SaveChangesAsync();

        Assert.NotEqual(default, widget.CreatedAtUtc);
        Assert.Equal("actor-42", widget.CreatedBy);
        Assert.Null(widget.ModifiedAtUtc);
    }

    [Fact]
    public async Task Modifying_an_entity_stamps_modified_audit_fields()
    {
        await using var db = CreateContext(actorId: "actor-1");
        var widget = new Widget { Name = "Widget A" };
        db.Widgets.Add(widget);
        await db.SaveChangesAsync();

        widget.Name = "Widget A (renamed)";
        await db.SaveChangesAsync();

        Assert.NotNull(widget.ModifiedAtUtc);
        Assert.Equal("actor-1", widget.ModifiedBy);
    }

    [Fact]
    public async Task Saving_writes_one_audit_log_row_per_changed_entity()
    {
        await using var db = CreateContext();
        db.Widgets.Add(new Widget { Name = "Widget A" });

        await db.SaveChangesAsync();

        var log = Assert.Single(db.AuditLogs);
        Assert.Equal(nameof(Widget), log.EntityName);
        Assert.Equal("Added", log.Action);
        Assert.Equal("api-key-1", log.ActorId);
    }

    [Fact]
    public async Task Audit_log_payload_never_contains_sensitive_property_values()
    {
        await using var db = CreateContext();
        db.Widgets.Add(new Widget { Name = "Widget A", DateOfBirth = "1990-01-01" });

        await db.SaveChangesAsync();

        var log = Assert.Single(db.AuditLogs);
        Assert.DoesNotContain("1990-01-01", log.ChangesJson);
        Assert.DoesNotContain(nameof(Widget.DateOfBirth), log.ChangesJson);
        Assert.Contains("Widget A", log.ChangesJson);
    }

    [Fact]
    public async Task Audit_log_entries_are_never_audited_themselves()
    {
        await using var db = CreateContext();
        db.Widgets.Add(new Widget { Name = "Widget A" });

        await db.SaveChangesAsync();

        Assert.DoesNotContain(db.AuditLogs, log => log.EntityName == nameof(AuditLog));
    }
}
