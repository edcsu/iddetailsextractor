# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build                                    # build the whole solution
dotnet test                                     # run every test project (requires Docker — see below)
dotnet test tests/IdExtractor.Modules.Extraction.Tests   # unit tests: MRZ parser, field extractor, doc-type detector
dotnet test tests/IdExtractor.Modules.Documents.Tests     # unit tests: PDF report generation, DTO mapping
dotnet test tests/IdExtractor.Api.IntegrationTests         # full-stack tests via Testcontainers Postgres
dotnet test --filter "FullyQualifiedName~MrzParserTests"   # run a single test class
docker compose up --build                       # run the full stack (api + ocr-service + postgres)
```

`IdExtractor.Api.IntegrationTests` spins up a real (throwaway) Postgres container per run via
Testcontainers, so Docker must be running locally for `dotnet test` to succeed.

The Python OCR sidecar (`ocr-service/`) has its own test suite, independent of the .NET solution:

```bash
cd ocr-service
python3 -m venv .venv && source .venv/bin/activate
pip install -r requirements-dev.txt
pytest
```

Those tests stub out the OCR engine itself (`get_ocr_engine`), so they run without the
paddleocr/paddlepaddle packages installed — only `requirements-dev.txt`'s lighter deps are needed.

## Architecture

This is a **modular monolith**: one ASP.NET Core host, internally split into feature modules that
**never reference each other's projects**. The only allowed dependency direction is
`Modules/* → SharedKernel`; the host (`IdExtractor.Api`) references everything.

```
src/
  IdExtractor.Api/                      # composition root: Program.cs wires modules, auth,
                                         # versioning, OpenTelemetry, Serilog, rate limiting
  IdExtractor.SharedKernel/
    Entities/                           # Entity (GUIDv7 id), IAuditable, AuditableEntity
    Auditing/                           # AuditSaveChangesInterceptor, [Sensitive], ICurrentActor
    Modules/                            # IModule — the module registration contract
    Telemetry/                          # shared OpenTelemetry ActivitySource
    Contracts/Extraction/               # cross-module contracts (see below)
  Modules/
    IdExtractor.Modules.Extraction/     # OCR orchestration, MRZ parsing, field extraction
    IdExtractor.Modules.Documents/      # persistence (EF Core/Postgres), queries, PDF reports
ocr-service/                            # Python/FastAPI PaddleOCR sidecar, internal-only
tests/
  IdExtractor.Modules.Extraction.Tests/
  IdExtractor.Modules.Documents.Tests/
  IdExtractor.Api.IntegrationTests/     # WebApplicationFactory + Testcontainers + fake OCR
```

### The module-boundary rule

Modules communicate **only** through MediatR commands/notifications, never by calling each
other's classes directly. Concretely: `ExtractionModule` handles `ExtractDocumentCommand` and, on
success, publishes a `DocumentExtracted` notification; `DocumentsModule` is the only subscriber —
its `DocumentExtractedNotificationHandler` is how a result actually gets persisted.

Because a notification handler in one module needs the notification *type* at compile time, those
cross-module contracts (`ExtractDocumentCommand`, `DocumentExtracted`, the DTOs, `DocumentType`)
live in `SharedKernel/Contracts/Extraction/`, not inside the Extraction module's own project — that
was a deliberate fix partway through this repo's history after the naive placement (contracts
inside the publishing module) would have forced a direct Documents→Extraction project reference.
Follow the same pattern for any new cross-module contract: put it in SharedKernel, not in the
module that happens to define the command/notification.

Each module implements `IModule` (`AddModule` for DI registration, `MapEndpoints` for its minimal
API routes) and is instantiated explicitly in `Program.cs`'s `modules` array — there's no
assembly-scanning magic for module discovery.

### Request flow

`POST /api/v1/extractions` (mapped in `ExtractionModule`) → `ExtractDocumentCommand` → validate
upload (magic bytes, not the client's claimed content-type) → render PDF page 1 to an image if
needed → call `IOcrProvider` → parse an MRZ if one is found → extract remaining fields via
label heuristics → detect document type → publish `DocumentExtracted` → `DocumentsModule` persists
the record and the source image, keyed by the same GUIDv7 id the Extraction module minted (so the
HTTP response id and the stored record id always match without a round trip).

### Key conventions

- **Ids**: every persisted entity extends `SharedKernel.Entities.Entity`, which mints a GUIDv7 on
  construction (`Guid.CreateVersion7()`). Never use `Guid.NewGuid()` for a persisted entity.
- **Handlers return `Result<T>`** (Ardalis.Result), translated to HTTP responses via
  `.ToMinimalApiResult()` at the endpoint. Don't throw for expected failure cases (validation,
  not-found) — return the appropriate `Result` factory instead.
- **PII never reaches logs or audit payloads unredacted**: any entity property or DTO field that
  can hold extracted document data (names, document numbers, MRZ text, raw OCR text) is marked
  `[Sensitive]`. `AuditSaveChangesInterceptor` strips those from the audit log's JSON payload;
  `SensitiveDestructuringPolicy` strips them from anything Serilog logs with `@`. When adding a
  new field that can carry extracted personal data, mark it `[Sensitive]` — this is not optional.
- **MediatR is pinned to exactly `12.5.0`** (`Version="[12.5.0]"` in every `.csproj` that
  references it) — the last version free to use without a commercial license. Do not bump it.
- **OCR providers** implement `IOcrProvider` (`Modules/Extraction/Ocr/`). The active one is
  selected in `ExtractionModule.AddModule` based on `Ocr:Provider` config (`"Paddle"` by default,
  `"AzureVision"` optional). Add a new provider by implementing the interface and adding a branch
  there — nothing else in the pipeline needs to change.
- **PDF report generation** uses NetPdfKit — a small, low-adoption package whose public docs are
  unreliable/inconsistent. If you need to change `ExtractionReportGenerator`, verify API shapes by
  inspecting the installed assembly directly (`Assembly.LoadFrom` + reflection, or read the source
  under `~/.nuget/packages/netpdfkit/`) rather than trusting fetched documentation.
- **`AddApiVersioning().AddOpenApi()` / `MapOpenApi().WithDocumentPerVersion()`** don't exist in
  the installed `Asp.Versioning` 10.2.1 despite the bundled analyzer suggesting them (confirmed by
  compile error) — `IdExtractor.Api.csproj` suppresses `AV0029`/`AV0030` with a comment explaining
  why. `Program.cs` registers one `AddOpenApi("v1", ...)` document per API version manually
  instead; follow that pattern when a `v2` ships.

### Adding a new module

1. New class library under `src/Modules/`, referencing only `IdExtractor.SharedKernel`.
2. Implement `IModule`.
3. Add cross-module contracts it needs to publish/consume to `SharedKernel/Contracts/<ModuleName>/`.
4. Instantiate it in `Program.cs`'s `modules` array (this also gets its assembly picked up by the
   `AddMediatR` registration, since that scans `modules.Select(m => m.GetType().Assembly)`).
5. If it needs its own database schema, follow `DocumentsModule`'s pattern: its own `DbContext`
   with `HasDefaultSchema(...)`, registered via `AddDbContext` in `AddModule`, with the
   `AuditSaveChangesInterceptor` wired in.

### Known environment limitation

Running the real PaddleOCR engine (not the test stub) inside Docker on an ARM64 host under
virtualization can segfault PaddlePaddle's inference engine — a confirmed upstream bug (see
README.md), not something in this codebase. All application logic downstream of the OCR call is
covered by `IdExtractor.Api.IntegrationTests` using a fake `IOcrProvider`, unaffected by this.
