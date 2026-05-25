using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Shared.Errors;
using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Errors;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;
using NexoPostal.Auth.Services;
using Xunit;

namespace Nexopostal.Tests.Auth;

/// <summary>
/// Tests unitarios para <see cref="AuthService"/> verificando el patrón
/// Railway Oriented Programming con <see cref="Result{T,DomainError}"/>.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly TokenService _tokenService;
    private readonly IAuthService _service;

    public AuthServiceTests()
    {
        var inMemoryConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "test-secret-key-with-enough-length-32-bytes-abc",
            ["JwtSettings:Issuer"] = "nexopostal-test",
            ["JwtSettings:Audience"] = "nexopostal-tests",
            ["JwtSettings:ExpiryMinutes"] = "60",
            ["JwtSettings:RefreshTokenExpiryDays"] = "14"
        }).Build();

        _tokenService = new TokenService(inMemoryConfig);
        _service = new AuthService(_userRepo.Object, _tokenService, _emailService.Object,
            NullLogger<AuthService>.Instance);
    }

    // ─── LOGIN ───

    [Fact]
    public async Task LoginAsync_ConCredencialesInvalidas_DeberiaDevolverUnauthorizedError()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("noexiste@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await _service.LoginAsync(new LoginDto { Email = "noexiste@test.com", Password = "x" });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
        result.Error.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioBloqueado_DeberiaDevolverForbiddenError()
    {
        var user = NewUser();
        _userRepo.Setup(r => r.GetByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userRepo.Setup(r => r.CheckPasswordAsync(user, "ok")).ReturnsAsync(true);
        _userRepo.Setup(r => r.IsLockedOutAsync(user)).ReturnsAsync(true);

        var result = await _service.LoginAsync(new LoginDto { Email = user.Email!, Password = "ok" });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ForbiddenError>();
        result.Error.Code.Should().Be("USER_BLOCKED");
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioEliminado_DeberiaDevolverForbiddenError()
    {
        var user = NewUser();
        user.Eliminado = true;
        _userRepo.Setup(r => r.GetByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userRepo.Setup(r => r.CheckPasswordAsync(user, "ok")).ReturnsAsync(true);

        var result = await _service.LoginAsync(new LoginDto { Email = user.Email!, Password = "ok" });

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("USER_BLOCKED");
    }

    [Fact]
    public async Task LoginAsync_CorrectoDeberiaEmitirTokenPair()
    {
        var user = NewUser();
        _userRepo.Setup(r => r.GetByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userRepo.Setup(r => r.CheckPasswordAsync(user, "ok")).ReturnsAsync(true);
        _userRepo.Setup(r => r.IsLockedOutAsync(user)).ReturnsAsync(false);

        var result = await _service.LoginAsync(new LoginDto { Email = user.Email!, Password = "ok" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.Rol.Should().Be("Cliente");
        _userRepo.Verify(r => r.SetUserTokenAsync(user, "NexoPostal", "RefreshTokenHash", It.IsAny<string>()), Times.Once);
        _userRepo.Verify(r => r.SetUserTokenAsync(user, "NexoPostal", "RefreshTokenExpiryUtc", It.IsAny<string>()), Times.Once);
    }

    // ─── REGISTER ───

    [Fact]
    public async Task RegisterAsync_ConEmailDuplicado_DeberiaDevolverConflictError()
    {
        var existente = NewUser();
        _userRepo.Setup(r => r.GetByEmailAsync(existente.Email!)).ReturnsAsync(existente);

        var result = await _service.RegisterAsync(new RegisterDto
        {
            Email = existente.Email!, Password = "secreta123", NombreCompleto = "Otra Persona"
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error.Code.Should().Be("EMAIL_IN_USE");
    }

    [Fact]
    public async Task RegisterAsync_ConIdentityFallido_DeberiaDevolverValidationError()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("nuevo@test.com")).ReturnsAsync((ApplicationUser?)null);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "PasswordTooShort" }));

        var result = await _service.RegisterAsync(new RegisterDto
        {
            Email = "nuevo@test.com", Password = "weak", NombreCompleto = "Nuevo"
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).ValidationErrors.Should().ContainKey("identity");
    }

    [Fact]
    public async Task RegisterAsync_Correcto_DeberiaEmitirTokenPair()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("nuevo@test.com")).ReturnsAsync((ApplicationUser?)null);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<ApplicationUser>(), "secreta123"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.RegisterAsync(new RegisterDto
        {
            Email = "nuevo@test.com", Password = "secreta123", NombreCompleto = "Nuevo Usuario"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Should().Be("Nuevo Usuario");
    }

    // ─── GET USER INFO ───

    [Fact]
    public async Task GetUserInfoAsync_ConUsuarioInexistente_DeberiaDevolverNotFoundError()
    {
        _userRepo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var result = await _service.GetUserInfoAsync("missing");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error.Code.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task GetUserInfoAsync_ConUsuarioExistente_DeberiaMapearADto()
    {
        var user = NewUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _service.GetUserInfoAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.NombreCompleto.Should().Be(user.NombreCompleto);
        result.Value.Rol.Should().Be("Cliente");
    }

    // ─── UPDATE PROFILE ───

    [Fact]
    public async Task UpdateProfileAsync_ConEmailEnUsoOtroUsuario_DeberiaDevolverConflictError()
    {
        var user = NewUser();
        var otro = NewUser("otro-id", "otro@test.com");
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _userRepo.Setup(r => r.GetByEmailAsync("otro@test.com")).ReturnsAsync(otro);

        var result = await _service.UpdateProfileAsync(user.Id,
            new ActualizarUsuarioDto { NombreCompleto = user.NombreCompleto, Email = "otro@test.com" });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    // ─── CHANGE PASSWORD ───

    [Fact]
    public async Task ChangePasswordAsync_ConUsuarioInexistente_DeberiaDevolverNotFoundError()
    {
        _userRepo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var result = await _service.ChangePasswordAsync("missing",
            new CambiarPasswordDto { PasswordActual = "x", NuevaPassword = "y" });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    [Fact]
    public async Task ChangePasswordAsync_Exitoso_DeberiaRevocarRefreshTokens()
    {
        var user = NewUser();
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _userRepo.Setup(r => r.ChangePasswordAsync(user, "old", "new"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.ChangePasswordAsync(user.Id,
            new CambiarPasswordDto { PasswordActual = "old", NuevaPassword = "new" });

        result.IsSuccess.Should().BeTrue();
        _userRepo.Verify(r => r.RemoveUserTokenAsync(user, "NexoPostal", "RefreshTokenHash"), Times.Once);
        _userRepo.Verify(r => r.RemoveUserTokenAsync(user, "NexoPostal", "RefreshTokenExpiryUtc"), Times.Once);
    }

    // ─── REFRESH TOKEN ───

    [Fact]
    public async Task RefreshTokenAsync_ConTokenVacio_DeberiaDevolverInvalidRefresh()
    {
        var result = await _service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "" });
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_REFRESH_TOKEN");
    }

    [Fact]
    public async Task RefreshTokenAsync_ConTokenMalformado_DeberiaDevolverInvalidRefresh()
    {
        var result = await _service.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = "not-a-base64-token" });
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("INVALID_REFRESH_TOKEN");
    }

    // ─── RESET PASSWORD ───

    [Fact]
    public async Task ResetPasswordAsync_ConEmailDesconocido_DeberiaDevolverBusinessRuleError()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("noexiste@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await _service.ResetPasswordAsync(new ResetPasswordDto
        {
            Email = "noexiste@test.com", Token = "t", NuevaPassword = "p"
        });

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessRuleError>();
    }

    [Fact]
    public async Task SolicitarResetPasswordAsync_ConEmailDesconocido_NoLevantaExcepcion()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("noexiste@test.com")).ReturnsAsync((ApplicationUser?)null);

        var act = () => _service.SolicitarResetPasswordAsync("noexiste@test.com", "http://localhost:4200");

        await act.Should().NotThrowAsync();
        _emailService.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    private static ApplicationUser NewUser(string id = "user-1", string email = "test@example.com") => new()
    {
        Id = id,
        UserName = email,
        Email = email,
        NombreCompleto = "Test User",
        Rol = Rol.Cliente
    };
}
