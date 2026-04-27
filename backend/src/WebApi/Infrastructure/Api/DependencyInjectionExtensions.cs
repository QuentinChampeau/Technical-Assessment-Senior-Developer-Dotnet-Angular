using Microsoft.EntityFrameworkCore;
using WebApi.Common.Caching;
using WebApi.Repositories;
using WebApi.Services;

namespace WebApi.Infrastructure.Api;

internal static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddControllers();

        #region Repositories
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        #endregion

        #region Services
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        #endregion

        services.AddEndpointsApiExplorer();
        // services.AddSwaggerGen();

        return services;
    }
}