using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdExtractor.SharedKernel.Modules;

/// <summary>
/// Registration contract every feature module implements. The API host discovers and
/// wires up modules through this interface only — modules never reference each other
/// directly, they communicate through MediatR.
/// </summary>
public interface IModule
{
    /// <summary>Registers the module's services (DbContext, handlers, providers, etc.).</summary>
    void AddModule(IServiceCollection services, IConfiguration configuration);

    /// <summary>Maps the module's minimal API endpoints.</summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
