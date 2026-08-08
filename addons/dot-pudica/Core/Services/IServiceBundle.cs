namespace DotPudica.Core.Services;

public interface IServiceBundle
{
    void RegisterServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services);
}
