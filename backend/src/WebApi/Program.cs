using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Infrastructure.Api;
using WebApi.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

/// Infrastructure
builder.AddRedisClient(connectionName: "cache"); // Redis cache layer
builder.AddNpgsqlDbContext<AppDbContext>(connectionName: "database"); // PostgreSQL relational database

// OpenAPI/Swagger
builder.Services
    .AddOpenApi()
    .AddPersistence();

// Health checks
builder.Services.AddHealthChecks();

// CORS to allow Angular frontend to call the API
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// API validation behavior
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        // Attach request trace identifier to simplify debugging.
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problemDetails);
    };
});

// CQRS with MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();

/// Global exception handling
// Centralized exception middleware used to:
// - log unhandled exceptions
// - convert known exceptions into structured ProblemDetails
// - avoid leaking raw exception details to API consumers
app.UseMiddleware<GlobalExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    try
    {
        // Automatic database migrations
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

    app.MapScalarApiReference(options =>
    {
        options.Title = "Expense Management API";
    });
}

// Middleware pipeline
app.UseHttpsRedirection();
app.UseCors("AngularFrontend");

// Endpoint mappings
app.MapUserEndpoints(); // TODO remove?
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class Program;