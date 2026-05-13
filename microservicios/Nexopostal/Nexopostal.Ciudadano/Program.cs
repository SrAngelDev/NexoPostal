using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Nexopostal.Ciudadano.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using System.Text;
using System.Reflection;

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
builder.Services.AddDbContext<CiudadanoDbContext>(options =>
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
            ClockSkew = TimeSpan.Zero // Eliminar el margen de 5 minutos por defecto
        };

        // Logging para debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Autenticación JWT fallida: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Token JWT validado para usuario: {UserId}", 
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// 3. Registrar repositorios (patrón repositorio)
builder.Services.AddScoped<IEnvioRepository, EnvioRepository>();
builder.Services.AddScoped<IClientePerfilRepository, ClientePerfilRepository>();

// 4. Registrar servicios propios
builder.Services.AddSingleton<OficinasJsonService>();
builder.Services.AddScoped<ITrackingNumberGenerator, TrackingNumberGenerator>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IEtiquetaPdfService, EtiquetaPdfService>();
builder.Services.AddScoped<IFacturaPdfService, FacturaPdfService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 3.1. Registrar servicio de notificación a Intranet (logística)
var intranetBaseUrl = ResolveConfigValue(builder.Configuration["IntranetSettings:BaseUrl"]);
if (string.IsNullOrWhiteSpace(intranetBaseUrl))
{
    intranetBaseUrl = "http://localhost:5163";
}
builder.Services.AddHttpClient<ILogisticaNotifierService, LogisticaNotifierService>(client =>
{
    client.BaseAddress = new Uri(intranetBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// 3.2. Registrar servicio de notificaciones de tracking en tiempo real
builder.Services.AddScoped<ITrackingNotificacionService, TrackingNotificacionService>();

// 3.3. Registrar servicios de automatización
builder.Services.AddScoped<IProcesoDevolucionService, ProcesoDevolucionService>();
builder.Services.AddScoped<INotificacionClienteService, NotificacionClienteService>();

// Background Services
builder.Services.AddHostedService<LimpiezaAutomaticaService>();
builder.Services.AddHostedService<ProcesadorPagosService>();

// 3.3. Configurar SignalR para tracking en tiempo real de envíos
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// 4. Configurar Controllers y JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // camelCase para que coincida con las interfaces TypeScript de Angular
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 5. Configurar OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // A. Metadatos del Proyecto
    c.SwaggerDoc("v1", new OpenApiInfo
    { 
        Title = "NexoPostal - Módulo Ciudadano",
        Version = "v1",
        Description = "API de Front-Office para NexoPostal: Admisión de Envíos, Cálculo de Tarifas y Trazabilidad.",
        Contact = new OpenApiContact
        {
            Name = "Ángel Sánchez Gasanz",
            Email = "estudiante@iesluisvives.org"
        }
    });

    // B. Definición de Seguridad (Botón "Authorize" con el candado)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce el token JWT obtenido en el login. Ejemplo: Bearer eyJhbGciOiJIUz..."
    });

    // C. Requisito de Seguridad (Aplica el candado a los endpoints)
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

    // D. Inclusión de comentarios XML (Documentación de código)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 6. Configurar CORS (para desarrollo)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://localhost:4200",  // Angular clientes-app
                "https://localhost:4201",  // Angular driver-app
                "https://localhost:4202",  // Angular intranet-app
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

// 1. Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexoPostal Ciudadano API v1");
        c.RoutePrefix = string.Empty;
    });
}

// 2. HTTPS Redirection
app.UseHttpsRedirection();

// 3. CORS
app.UseCors("AllowFrontend");

// 4. Authentication & Authorization (orden importante)
app.UseAuthentication();
app.UseAuthorization();

// 5. Mapear Controllers
app.MapControllers();

// 6. Mapear el Hub de SignalR para tracking en tiempo real de envíos
app.MapHub<TrackingHub>("/hubs/tracking");

// ===== INICIALIZACIÓN DE BASE DE DATOS =====

// Aplicar migraciones automáticamente (incluye producción)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CiudadanoDbContext>();

    try
    {
        await dbContext.Database.MigrateAsync();
        app.Logger.LogInformation("Migraciones aplicadas correctamente");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al aplicar migraciones de base de datos");
    }
}

// ===== LOGGING DE INICIO =====

app.Logger.LogInformation("NexoPostal.Ciudadano API iniciada correctamente");
app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);

// Enmascarar password en el log de forma segura
var logConnectionString = connectionString ?? "N/A";
var passwordIndex = logConnectionString.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
if (passwordIndex >= 0)
{
    var endIndex = logConnectionString.IndexOf(';', passwordIndex);
    logConnectionString = endIndex >= 0
        ? string.Concat(logConnectionString.AsSpan(0, passwordIndex), "Password=***", logConnectionString.AsSpan(endIndex))
        : string.Concat(logConnectionString.AsSpan(0, passwordIndex), "Password=***");
}
app.Logger.LogInformation("Base de datos: {ConnectionString}", logConnectionString);

app.Run();
