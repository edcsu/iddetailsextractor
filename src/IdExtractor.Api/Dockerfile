# Build context is the repo root (see docker-compose.yml) so relative
# ProjectReferences between the host, SharedKernel, and the modules resolve.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/IdExtractor.Api/IdExtractor.Api.csproj src/IdExtractor.Api/
COPY src/IdExtractor.SharedKernel/IdExtractor.SharedKernel.csproj src/IdExtractor.SharedKernel/
COPY src/Modules/IdExtractor.Modules.Extraction/IdExtractor.Modules.Extraction.csproj src/Modules/IdExtractor.Modules.Extraction/
COPY src/Modules/IdExtractor.Modules.Documents/IdExtractor.Modules.Documents.csproj src/Modules/IdExtractor.Modules.Documents/
RUN dotnet restore src/IdExtractor.Api/IdExtractor.Api.csproj

COPY src/ src/
RUN dotnet publish src/IdExtractor.Api/IdExtractor.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "IdExtractor.Api.dll"]
