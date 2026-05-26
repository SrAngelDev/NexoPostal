using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Nexopostal.Shared.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

static string ResolveConfigValue(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;
    return Regex.Replace(value, @"\$\{([^}]+)\}", match =>
        Environment.GetEnvironmentVariable(match.Groups[1].Value) ?? match.Value);
}

static byte[] GetJwtKeyBytes(string secret)
{
    var keyBytes = Encoding.UTF8.GetBytes(secret);
    return keyBytes.Length >= 32 ? keyBytes : SHA256.HashData(keyBytes);
}

// ===== CONFIGURACIÓN DE SERVICIOS =====

// 1. Configurar DbContext con PostgreSQL
var connectionString = ResolveConfigValue(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddDbContext<RepartoDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Configurar JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = ResolveConfigValue(jwtSettings["SecretKey"]);
if (string.IsNullOrWhiteSpace(secretKey)) throw new InvalidOperationException("JWT SecretKey no configurada");

var issuer = ResolveConfigValue(jwtSettings["Issuer"]);
var audience = ResolveConfigValue(jwtSettings["Audience"]);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(GetJwtKeyBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Autenticación JWT fallida: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            // Permitir token vía query string para SignalR (WebSocket no soporta headers custom).
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/reparto"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// 3. Registrar repositorios
builder.Services.AddScoped<IRepartidorRepository, RepartidorRepository>();
builder.Services.AddScoped<IRutaRepartoRepository, RutaRepartoRepository>();
builder.Services.AddScoped<IEntregaPaqueteRepository, EntregaPaqueteRepository>();
builder.Services.AddScoped<IUbicacionRepartidorRepository, UbicacionRepartidorRepository>();

// 4. Registrar servicios propios
builder.Services.AddScoped<IRepartoService, RepartoService>();
builder.Services.AddScoped<IOptimizacionRutasService, OptimizacionRutasService>();
builder.Services.AddScoped<IReintentoEntregaService, ReintentoEntregaService>();
builder.Services.AddScoped<IBalanceoCargaService, BalanceoCargaService>();
builder.Services.AddScoped<IVehiculoRepository, VehiculoRepository>();
builder.Services.AddScoped<IVehiculoService, VehiculoService>();
builder.Services.AddScoped<IBandejaPendientesService, BandejaPendientesService>();

// Notificador SignalR (singleton: usa IHubContext que es thread-safe)
builder.Services.AddSingleton<IRepartoNotifier, RepartoNotifier>();
builder.Services.AddSignalR();

var ciudadanoTrackingBaseUrl = ResolveConfigValue(builder.Configuration["CiudadanoTrackingSettings:BaseUrl"]);
// Fallback: vacío o placeholder sin resolver (${...}) — usar valor por defecto en red Docker.
if (string.IsNullOrWhiteSpace(ciudadanoTrackingBaseUrl) || ciudadanoTrackingBaseUrl.Contains("${"))
{
    ciudadanoTrackingBaseUrl = "http://modulo-ciudadano:80";
}

builder.Services.AddHttpClient<ICiudadanoTrackingNotifierService, CiudadanoTrackingNotifierService>(client =>
{
    client.BaseAddress = new Uri(ciudadanoTrackingBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// 4. Configurar Controllers y JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 5. Configurar OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NexoPostal - Módulo Reparto (Última Milla)",
        Version = "v1",
        Description = "API de Reparto para NexoPostal: Gestión de repartidores, rutas de reparto y entregas a domicilio.",
        Contact = new OpenApiContact
        {
            Name = "Ángel Sánchez Gasanz",
            Email = "estudiante@iesluisvives.org"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce el token JWT obtenido en el login."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 6. Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://localhost:4200",
                "https://localhost:4201",
                "https://localhost:4202",
                "https://nexopostal.es",
                "https://*.nexopostal.es"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// ===== CONSTRUCCIÓN DE LA APLICACIÓN =====

var app = builder.Build();

// ===== CONFIGURACIÓN DEL PIPELINE HTTP =====

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexoPostal Reparto API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseGlobalExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Nexopostal.Reparto.Hubs.RepartoHub>("/hubs/reparto");

// ===== INICIALIZACIÓN DE BASE DE DATOS =====
// Migraciones y seeding se aplican en TODOS los entornos (el seeder es idempotente).

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RepartoDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Migraciones de Reparto aplicadas correctamente");

        // El seeder es idempotente (chequea AnyAsync antes de insertar), se ejecuta en cualquier entorno.
        await RepartoDataSeeder.SeedAsync(dbContext, logger, app.Environment);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al inicializar la base de datos de Reparto");
    }
}

// ===== LOGGING DE INICIO =====

app.Logger.LogInformation("NexoPostal.Reparto API iniciada correctamente");
app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);

app.Run();
