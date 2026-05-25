using Nexopostal.Shared.Infrastructures;
using Nexopostal.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using NexoPostal.Auth.Data;
using NexoPostal.Auth.Infrastructures;

var builder = WebApplication.CreateBuilder(args);

// Logging unificado con Serilog
builder.AddNexopostalSerilog("Nexopostal.Auth");

// Módulos de infraestructura
builder.Services
    .AddAuthDatabase(builder.Configuration)
    .AddAuthIdentity()
    .AddAuthJwt(builder.Configuration)
    .AddAuthRepositories()
    .AddAuthServices()
    .AddAuthValidation();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Aplicar migraciones automáticamente y seeding
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedData.Initialize(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware global de excepciones (debe ir antes que el resto)
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

/// <summary>
/// Marca pública para que WebApplicationFactory&lt;Program&gt; pueda referenciar este host en los tests.
/// </summary>
public partial class Program { }
