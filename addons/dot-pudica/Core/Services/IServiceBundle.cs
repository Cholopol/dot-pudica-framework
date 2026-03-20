namespace DotPudica.Core.Services;

/// <summary>
/// Service registration extension interface. Components can implement this interface to describe their own required registrations.
/// </summary>
public interface IServiceBundle
{
    /// <summary>
    /// Register all services of this module to the service collection.
    /// </summary>
    void RegisterServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services);
}
