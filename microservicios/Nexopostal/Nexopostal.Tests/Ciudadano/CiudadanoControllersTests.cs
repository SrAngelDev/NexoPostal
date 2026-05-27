using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Ciudadano.Controllers;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using System.Security.Claims;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

internal static class CiudadanoControllerTestExtensions
{
    public static void WireUser(this ControllerBase ctrl, string? userId = "user-1", string role = "Cliente")
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user };
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
    }
}

public class AdminClientesControllerTests
{
    private readonly Mock<IClientePerfilRepository> _perfilRepo = new();
    private readonly Mock<IEnvioRepository> _envioRepo = new();
    private readonly AdminClientesController _ctrl;

    public AdminClientesControllerTests()
    {
        _ctrl = new AdminClientesController(_perfilRepo.Object, _envioRepo.Object);
        _ctrl.WireUser(role: "Admin");
    }

    [Fact]
    public async Task PerfilCompleto_ConPerfilYEnvios_AgregaEstadisticas()
    {
        var perfil = new ClientePerfil
        {
            Id = 1,
            IdentityUserId = "u1",
            DNI = "12345678A",
            Telefono = "600000000",
            DireccionPredeterminada = "C/ X",
            FechaCreacion = DateTime.UtcNow,
            Agenda = new List<DireccionFavorita>
            {
                new() { Id = 10, Alias = "Casa", NombreDestinatario = "Yo", Direccion = "Calle 1", CodigoPostal = "28001", Ciudad = "Madrid", Provincia = "Madrid", Telefono = "1" }
            }
        };
        var envios = new List<Envio>
        {
            new() { NumeroSeguimiento = "NXP-1", Pagado = true, EstadoActual = EstadoEnvio.Entregado, CosteCalculado = 10m },
            new() { NumeroSeguimiento = "NXP-2", Pagado = false, EstadoActual = EstadoEnvio.Admitido, CosteCalculado = 5m },
            new() { NumeroSeguimiento = "NXP-3", Pagado = true, EstadoActual = EstadoEnvio.Incidencia, CosteCalculado = 7m }
        };
        _perfilRepo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(perfil);
        _envioRepo.Setup(r => r.GetByUserAsync("u1")).ReturnsAsync(envios);

        var result = await _ctrl.PerfilCompleto("u1");
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PerfilCompleto_SinPerfil_DevuelveOkConPerfilNull()
    {
        _perfilRepo.Setup(r => r.GetByUserIdAsync("u9")).ReturnsAsync((ClientePerfil?)null);
        _envioRepo.Setup(r => r.GetByUserAsync("u9")).ReturnsAsync(new List<Envio>());

        var result = await _ctrl.PerfilCompleto("u9");
        result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1000)]
    public async Task PerfilCompleto_MaxEnviosInvalido_UsaDefault(int maxEnvios)
    {
        _perfilRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>())).ReturnsAsync((ClientePerfil?)null);
        _envioRepo.Setup(r => r.GetByUserAsync(It.IsAny<string>())).ReturnsAsync(new List<Envio>());

        var result = await _ctrl.PerfilCompleto("u1", maxEnvios);
        result.Should().BeOfType<OkObjectResult>();
    }
}

public class AdminEnviosControllerTests
{
    private readonly Mock<IAdminEnviosService> _svc = new();
    private readonly AdminEnviosController _ctrl;

    public AdminEnviosControllerTests()
    {
        _ctrl = new AdminEnviosController(_svc.Object);
        _ctrl.WireUser(userId: "admin-1", role: "Admin");
    }

    [Fact]
    public async Task Listar_DevuelveOkConListado()
    {
        _svc.Setup(s => s.ListarAsync(null, null, null, null, null, null, null, 500))
            .ReturnsAsync(new List<AdminEnvioListItemDto> { new() { NumeroSeguimiento = "NXP-1" } });

        var r = await _ctrl.Listar(null, null, null, null, null, null, null);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Obtener_Existente_DevuelveOk()
    {
        _svc.Setup(s => s.ObtenerAsync("NXP-1")).ReturnsAsync(new AdminEnvioDetalleDto { NumeroSeguimiento = "NXP-1" });
        var r = await _ctrl.Obtener("NXP-1");
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Obtener_Inexistente_DevuelveNotFound()
    {
        _svc.Setup(s => s.ObtenerAsync("NXP-X")).ReturnsAsync((AdminEnvioDetalleDto?)null);
        var r = await _ctrl.Obtener("NXP-X");
        r.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CambiarEstado_Ok()
    {
        _svc.Setup(s => s.CambiarEstadoAsync("NXP-1", It.IsAny<CambiarEstadoEnvioDto>(), "admin-1"))
            .ReturnsAsync((new AdminEnvioDetalleDto { NumeroSeguimiento = "NXP-1" }, (string?)null));
        var r = await _ctrl.CambiarEstado("NXP-1", new CambiarEstadoEnvioDto());
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CambiarEstado_NotFound()
    {
        _svc.Setup(s => s.CambiarEstadoAsync(It.IsAny<string>(), It.IsAny<CambiarEstadoEnvioDto>(), It.IsAny<string?>()))
            .ReturnsAsync((null, "Envío no encontrado"));
        var r = await _ctrl.CambiarEstado("NXP-X", new CambiarEstadoEnvioDto());
        r.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CambiarEstado_Conflicto()
    {
        _svc.Setup(s => s.CambiarEstadoAsync(It.IsAny<string>(), It.IsAny<CambiarEstadoEnvioDto>(), It.IsAny<string?>()))
            .ReturnsAsync((null, "Estado inválido"));
        var r = await _ctrl.CambiarEstado("NXP-1", new CambiarEstadoEnvioDto());
        r.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CambiarEstado_ModelStateInvalid_DevuelveBadRequest()
    {
        _ctrl.ModelState.AddModelError("k", "err");
        var r = await _ctrl.CambiarEstado("NXP-1", new CambiarEstadoEnvioDto());
        r.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Anular_Ok_NotFound_Conflict()
    {
        _svc.SetupSequence(s => s.AnularAsync(It.IsAny<string>(), It.IsAny<AccionEnvioDto>(), It.IsAny<string?>()))
            .ReturnsAsync((new AdminEnvioDetalleDto { NumeroSeguimiento = "NXP-1" }, null))
            .ReturnsAsync((null, "Envío no encontrado"))
            .ReturnsAsync((null, "Ya anulado"));

        (await _ctrl.Anular("NXP-1", new AccionEnvioDto())).Result.Should().BeOfType<OkObjectResult>();
        (await _ctrl.Anular("NXP-1", new AccionEnvioDto())).Result.Should().BeOfType<NotFoundObjectResult>();
        (await _ctrl.Anular("NXP-1", new AccionEnvioDto())).Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Reabrir_Ok_NotFound_Conflict()
    {
        _svc.SetupSequence(s => s.ReabrirAsync(It.IsAny<string>(), It.IsAny<AccionEnvioDto>(), It.IsAny<string?>()))
            .ReturnsAsync((new AdminEnvioDetalleDto { NumeroSeguimiento = "NXP-1" }, null))
            .ReturnsAsync((null, "Envío no encontrado"))
            .ReturnsAsync((null, "No se puede reabrir"));

        (await _ctrl.Reabrir("NXP-1", new AccionEnvioDto())).Result.Should().BeOfType<OkObjectResult>();
        (await _ctrl.Reabrir("NXP-1", new AccionEnvioDto())).Result.Should().BeOfType<NotFoundObjectResult>();
        (await _ctrl.Reabrir("NXP-1", new AccionEnvioDto())).Result.Should().BeOfType<ConflictObjectResult>();
    }
}

public class PerfilControllerTests
{
    private readonly Mock<IClientePerfilRepository> _repo = new();
    private readonly PerfilController _ctrl;

    public PerfilControllerTests()
    {
        _ctrl = new PerfilController(_repo.Object, NullLogger<PerfilController>.Instance);
        _ctrl.WireUser("user-1");
    }

    [Fact]
    public async Task GetPerfil_SinToken_Unauthorized()
    {
        var c = new PerfilController(_repo.Object, NullLogger<PerfilController>.Instance);
        c.WireUser(userId: null);
        var r = await c.GetPerfil();
        r.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetPerfil_PerfilExistente_DevuelveOk()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1"))
            .ReturnsAsync(new ClientePerfil { IdentityUserId = "user-1", DNI = "X", FechaCreacion = DateTime.UtcNow });
        var r = await _ctrl.GetPerfil();
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPerfil_PerfilVacio_DevuelveOkConDefaults()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync((ClientePerfil?)null);
        var r = await _ctrl.GetPerfil();
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CrearOActualizar_CreaNuevo_CuandoNoExiste()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync((ClientePerfil?)null);
        _repo.Setup(r => r.CreateOrUpdateAsync(It.IsAny<ClientePerfil>())).ReturnsAsync((ClientePerfil p) => p);

        var r = await _ctrl.CrearOActualizarPerfil(new ActualizarPerfilDto { DNI = "12345678A" });
        r.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CrearOActualizar_Actualiza_CuandoExiste()
    {
        var perfilExistente = new ClientePerfil { IdentityUserId = "user-1", DNI = "old", FechaCreacion = DateTime.UtcNow };
        _repo.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync(perfilExistente);
        _repo.Setup(r => r.CreateOrUpdateAsync(It.IsAny<ClientePerfil>())).ReturnsAsync((ClientePerfil p) => p);

        var r = await _ctrl.CrearOActualizarPerfil(new ActualizarPerfilDto { Telefono = "600" });
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CrearOActualizar_ModelStateInvalid_BadRequest()
    {
        _ctrl.ModelState.AddModelError("k", "err");
        var r = await _ctrl.CrearOActualizarPerfil(new ActualizarPerfilDto());
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetDirecciones_SinPerfil_ListaVacia()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync((ClientePerfil?)null);
        var r = await _ctrl.GetDireccionesFavoritas();
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDirecciones_ConPerfil_DevuelveLista()
    {
        var perfil = new ClientePerfil
        {
            Id = 1, IdentityUserId = "user-1",
            Agenda = new List<DireccionFavorita>
            {
                new() { Id = 1, Alias = "C", NombreDestinatario = "X", Direccion = "Y", CodigoPostal = "28001", Ciudad = "M", Provincia = "M" }
            }
        };
        _repo.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync(perfil);
        var r = await _ctrl.GetDireccionesFavoritas();
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Agregar_CreaPerfilSiNoExiste()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync((ClientePerfil?)null);
        _repo.Setup(r => r.CreateOrUpdateAsync(It.IsAny<ClientePerfil>())).ReturnsAsync((ClientePerfil p) => { p.Id = 99; return p; });
        _repo.Setup(r => r.AddDireccionAsync(It.IsAny<DireccionFavorita>())).ReturnsAsync((DireccionFavorita d) => d);

        var dto = new CrearDireccionFavoritaDto
        {
            Alias = "Casa", NombreDestinatario = "Y", Direccion = "C/1",
            CodigoPostal = "28001", Ciudad = "M", Provincia = "M"
        };
        var r = await _ctrl.AgregarDireccionFavorita(dto);
        r.Should().BeOfType<CreatedAtActionResult>();
        _repo.Verify(x => x.CreateOrUpdateAsync(It.IsAny<ClientePerfil>()), Times.Once);
    }

    [Fact]
    public async Task Agregar_ModelStateInvalid_BadRequest()
    {
        _ctrl.ModelState.AddModelError("k", "err");
        var r = await _ctrl.AgregarDireccionFavorita(new CrearDireccionFavoritaDto());
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Actualizar_NoExisteDireccion_NotFound()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1"))
            .ReturnsAsync(new ClientePerfil { Id = 1, IdentityUserId = "user-1" });
        _repo.Setup(r => r.GetDireccionByIdAsync(99, 1)).ReturnsAsync((DireccionFavorita?)null);

        var dto = new CrearDireccionFavoritaDto { Alias = "X", NombreDestinatario = "Y", Direccion = "Z", CodigoPostal = "28001", Ciudad = "M", Provincia = "M" };
        var r = await _ctrl.ActualizarDireccionFavorita(99, dto);
        r.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Actualizar_ExistePerfilYDireccion_Ok()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1"))
            .ReturnsAsync(new ClientePerfil { Id = 1, IdentityUserId = "user-1" });
        _repo.Setup(r => r.GetDireccionByIdAsync(7, 1))
            .ReturnsAsync(new DireccionFavorita { Id = 7, ClientePerfilId = 1, Alias = "a", NombreDestinatario = "b", Direccion = "c", CodigoPostal = "28001", Ciudad = "d", Provincia = "e" });

        var dto = new CrearDireccionFavoritaDto { Alias = "X", NombreDestinatario = "Y", Direccion = "Z", CodigoPostal = "28001", Ciudad = "M", Provincia = "M" };
        var r = await _ctrl.ActualizarDireccionFavorita(7, dto);
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Actualizar_SinPerfil_NotFound()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync((ClientePerfil?)null);
        var dto = new CrearDireccionFavoritaDto { Alias = "X", NombreDestinatario = "Y", Direccion = "Z", CodigoPostal = "28001", Ciudad = "M", Provincia = "M" };
        var r = await _ctrl.ActualizarDireccionFavorita(7, dto);
        r.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Eliminar_OK_DevuelveNoContent()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1"))
            .ReturnsAsync(new ClientePerfil { Id = 1, IdentityUserId = "user-1" });
        _repo.Setup(r => r.DeleteDireccionAsync(7, 1)).ReturnsAsync(true);
        var r = await _ctrl.EliminarDireccionFavorita(7);
        r.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Eliminar_DireccionInexistente_NotFound()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1"))
            .ReturnsAsync(new ClientePerfil { Id = 1, IdentityUserId = "user-1" });
        _repo.Setup(r => r.DeleteDireccionAsync(99, 1)).ReturnsAsync(false);
        var r = await _ctrl.EliminarDireccionFavorita(99);
        r.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Eliminar_SinPerfil_NotFound()
    {
        _repo.Setup(r => r.GetByUserIdAsync("user-1")).ReturnsAsync((ClientePerfil?)null);
        var r = await _ctrl.EliminarDireccionFavorita(7);
        r.Should().BeOfType<NotFoundObjectResult>();
    }
}
