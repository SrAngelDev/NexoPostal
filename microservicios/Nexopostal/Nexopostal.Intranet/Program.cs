using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.Hubs;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
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
builder.Services.AddDbContext<IntranetDbContext>(options =>
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
            // Permitir token JWT en query string para SignalR (WebSocket no envía headers)
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/intranet"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
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

// 3. Registrar repositorios
builder.Services.AddScoped<ICentroTratamientoRepository, CentroTratamientoRepository>();
builder.Services.AddScoped<IRutaCtaRepository, RutaCtaRepository>();
builder.Services.AddScoped<IOperarioCtaRepository, OperarioCtaRepository>();
builder.Services.AddScoped<IOperarioOficinaRepository, OperarioOficinaRepository>();
builder.Services.AddScoped<IAsignacionPaqueteRepository, AsignacionPaqueteRepository>();
builder.Services.AddScoped<IMovimientoPaqueteRepository, MovimientoPaqueteRepository>();
builder.Services.AddScoped<IIncidenciaRepository, IncidenciaRepository>();
builder.Services.AddScoped<IHistorialEstadoRepository, HistorialEstadoRepository>();

// 4. Registrar servicios propios
builder.Services.AddScoped<IClasificacionService, ClasificacionService>();
builder.Services.AddScoped<IOperarioService, OperarioService>();
builder.Services.AddScoped<IAsignacionService, AsignacionService>();
builder.Services.AddScoped<IMovimientoService, MovimientoService>();
builder.Services.AddScoped<IIncidenciaService, IncidenciaService>();
builder.Services.AddScoped<IAdmisionService, AdmisionService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IHistorialService, HistorialService>();
builder.Services.AddSingleton<OficinasJsonService>();
builder.Services.AddScoped<IOficinaPostalService, OficinaPostalService>();
builder.Services.AddScoped<IScanProcessorService, ScanProcessorService>();

var repartoBaseUrl = ResolveConfigValue(builder.Configuration["RepartoSettings:BaseUrl"]);
if (string.IsNullOrWhiteSpace(repartoBaseUrl) || repartoBaseUrl.Contains("${"))
{
    repartoBaseUrl = "http://localhost:5300";
}

builder.Services.AddHttpClient<IRepartoOrquestacionService, RepartoOrquestacionService>(client =>
{
    client.BaseAddress = new Uri(repartoBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// 4.0.b Cliente inter-servicio hacia Ciudadano (lookup de envíos por expedición)
var ciudadanoBaseUrl = ResolveConfigValue(builder.Configuration["CiudadanoApi:BaseUrl"]);
if (string.IsNullOrWhiteSpace(ciudadanoBaseUrl) || ciudadanoBaseUrl.Contains("${"))
{
    ciudadanoBaseUrl = "http://localhost:5200";
}
var interServiceKey = ResolveConfigValue(builder.Configuration["InterServiceSettings:ServiceKey"]);
if (string.IsNullOrWhiteSpace(interServiceKey) || interServiceKey.Contains("${"))
{
    interServiceKey = "nexopostal-internal-service-key-2025";
}

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ICiudadanoEnvioLookupService, CiudadanoEnvioLookupService>(client =>
{
    client.BaseAddress = new Uri(ciudadanoBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.Add("X-Service-Key", interServiceKey);
});

// 4.1 Servicios de automatización
builder.Services.AddScoped<IClasificacionAutomaticaService, ClasificacionAutomaticaService>();
builder.Services.AddScoped<INotificacionAutomaticaService, NotificacionAutomaticaService>();
builder.Services.AddScoped<IGestionIncidenciasAutomaticaService, GestionIncidenciasAutomaticaService>();
builder.Services.AddScoped<IInformesAutomaticosService, InformesAutomaticosService>();

// 3.1b. Servicio de simulación de transporte (background)
builder.Services.AddHostedService<SimulacionTransporteService>();

// Background Services
builder.Services.AddHostedService<MonitorizacionSaludService>();

// 3.1. Configurar SignalR para notificaciones en tiempo real
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
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 5. Configurar OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NexoPostal - Módulo Intranet (Logística)",
        Version = "v1",
        Description = "API de Back-Office para NexoPostal: Gestión de CTAs, Operarios, Clasificación, Movimientos Troncales e Incidencias.",
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
        Description = "Introduce el token JWT obtenido en el login. Ejemplo: Bearer eyJhbGciOiJIUz..."
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NexoPostal Intranet API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Mapear el Hub de SignalR para notificaciones en tiempo real
app.MapHub<IntranetHub>("/hubs/intranet");

// ===== INICIALIZACIÓN DE BASE DE DATOS =====

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IntranetDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var oficinasService = scope.ServiceProvider.GetRequiredService<OficinasJsonService>();

    try
    {
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Migraciones aplicadas correctamente");

        // Ejecutar DataSeeder (idempotente: solo siembra si la BD está vacía)
        await IntranetDataSeeder.SeedAsync(dbContext, logger, oficinasService);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

// ===== LOGGING DE INICIO =====

app.Logger.LogInformation("NexoPostal.Intranet API iniciada correctamente");
app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);

app.Run();