using Microsoft.EntityFrameworkCore;
using WebApi.Common.Caching;

namespace WebApi.Infrastructure.Api;

internal static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddEndpointsApiExplorer();
        // services.AddSwaggerGen();

        return services;
    }
}