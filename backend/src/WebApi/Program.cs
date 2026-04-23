using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.Api;
using WebApi.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisClient(connectionName: "cache");
builder.AddNpgsqlDbContext<AppDbContext>(connectionName: "database");
builder.Services
    .AddOpenApi()
    .AddPersistence();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    try
    {
        using var sp = app.Services.CreateScope();
        await sp.ServiceProvider.GetRequiredService<AppDbContext>()
            .Database.MigrateAsync();
    }
    catch (Exception e)
    {
        app.Services.GetRequiredService<ILogger<Program>>()
            .LogError(e, "An error occurred while migrating the database.");
        return;
    } 
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//app.MapUserEndpoints(); // TODO remove?
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class Program;