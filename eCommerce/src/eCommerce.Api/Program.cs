using eCommerce.Api.Infrastructure;
using eCommerce.Application;
using eCommerce.Infrastructure;
using eCommerce.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Each layer registers its own services; composition happens only here.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    await using var scope = app.Services.CreateAsyncScope();
    var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

    await initialiser.MigrateAsync();
    await initialiser.SeedAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

/// <summary>Exposed so an integration test host can reference this entry point.</summary>
public partial class Program;
