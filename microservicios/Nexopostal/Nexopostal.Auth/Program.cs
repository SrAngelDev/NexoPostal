using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NexoPostal.Auth.Data;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;
using NexoPostal.Auth.Services;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

static string ResolveConfigValue(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

    return Regex.Replace(value, @"\$\{([^}]+)\}", match =>
    {
        var envVar = match.Groups[1].Value;
        return Environment.GetEnvironmentVariable(envVar) ?? match.Value;
    });
}

static byte[] GetJwtKeyBytes(string secret)
{
    var keyBytes = Encoding.UTF8.GetBytes(secret);
    return keyBytes.Length >= 32 ? keyBytes : SHA256.HashData(keyBytes);
}

var connectionString = ResolveConfigValue(builder.Configuration.GetConnectionString("DefaultConnection"));

// Entity Framework
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

// Identity (sin roles de Identity, usamos enum Rol en ApplicationUser)
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// JWT Bearer Authentication (para endpoints [Authorize])
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = ResolveConfigValue(jwtSettings["SecretKey"]);
if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException("JWT SecretKey no configurada");

var key = GetJwtKeyBytes(secretKey);
var issuer = ResolveConfigValue(jwtSettings["Issuer"]);
var audience = ResolveConfigValue(jwtSettings["Audience"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
