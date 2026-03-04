using AspNetCore.ApiGateway;
using Nexopostal.Gateway.Extensions;
using Nexopostal.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICIOS =====
builder.Services
    .AddGatewayCors(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddGatewayServices();

var app = builder.Build();

// PIPELINE
app.UseGatewayErrorHandling();
app.UseCors("NexoPostalPolicy");
app.UseUrlRewrite();
app.UseApiGateway(orchestrator => ApiOrchestrationConfig.ConfigureRoutes(orchestrator, app.Configuration));
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

