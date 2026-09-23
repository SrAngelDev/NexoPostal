using Nexopostal.Gateway.Extensions;
using Nexopostal.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICIOS =====
builder.Services
    .AddGatewayCors(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddGatewayServices(builder.Configuration);

var app = builder.Build();

// PIPELINE
app.UseGlobalExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("NexoPostalPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

