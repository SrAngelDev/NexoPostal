using System.Security.Claims;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nexopostal.Reparto.Controllers;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Tests.Reparto;

public class AdminVehiculosControllerTests
{
    private readonly Mock<IVehiculoService> _svc = new();

    private AdminVehiculosController CreateCtrl(string? userId = "admin-1")
    {
        var ctrl = new AdminVehiculosController(_svc.Object);
        var claims = new List<Claim>();
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        var identity = new ClaimsIdentity(claims, "TestAuth");
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return ctrl;
    }

    private static Vehiculo Sample(int id = 1) => new()
    {
        Id = id,
        Matricula = $"M{id:0000}AB",
        Tipo = TipoVehiculo.Furgoneta,
        Activo = true,
        FechaAlta = DateTime.UtcNow,
        FechaModificacion = DateTime.UtcNow
    };

    [Fact]
    public async Task Listar_OK()
    {
        _svc.Setup(s => s.ListarAsync(false, null, null))
            .ReturnsAsync(new List<Vehiculo> { Sample(1), Sample(2) });
        var res = await CreateCtrl().Listar();
        var ok = res.Result as OkObjectResult;
        ok!.Value.Should().BeOfType<List<VehiculoDto>>().Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task Obtener_NotFound()
    {
        _svc.Setup(s => s.ObtenerAsync(99)).ReturnsAsync((Vehiculo?)null);
        var res = await CreateCtrl().Obtener(99);
        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Obtener_OK()
    {
        _svc.Setup(s => s.ObtenerAsync(1)).ReturnsAsync(Sample(1));
        var res = await CreateCtrl().Obtener(1);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Crear_OK_DevuelveCreated()
    {
        var dto = new CrearVehiculoDto { Matricula = "1234ABC", Tipo = TipoVehiculo.Furgoneta };
        _svc.Setup(s => s.CrearAsync(dto, "admin-1"))
            .ReturnsAsync((Sample(5), null));
        var res = await CreateCtrl().Crear(dto);
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Crear_Conflict_CuandoError()
    {
        var dto = new CrearVehiculoDto { Matricula = "DUPLICADO", Tipo = TipoVehiculo.Furgoneta };
        _svc.Setup(s => s.CrearAsync(dto, "admin-1"))
            .ReturnsAsync(((Vehiculo?)null, "Matrícula duplicada"));
        var res = await CreateCtrl().Crear(dto);
        res.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Crear_BadRequest_CuandoModelStateInvalido()
    {
        var ctrl = CreateCtrl();
        ctrl.ModelState.AddModelError("Matricula", "obligatoria");
        var res = await ctrl.Crear(new CrearVehiculoDto());
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Actualizar_NotFound()
    {
        var dto = new ActualizarVehiculoDto { Matricula = "X", Tipo = TipoVehiculo.Furgoneta };
        _svc.Setup(s => s.ActualizarAsync(99, dto, "admin-1"))
            .ReturnsAsync(((Vehiculo?)null, "Vehículo no encontrado"));
        var res = await CreateCtrl().Actualizar(99, dto);
        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Actualizar_Conflict()
    {
        var dto = new ActualizarVehiculoDto { Matricula = "X", Tipo = TipoVehiculo.Furgoneta };
        _svc.Setup(s => s.ActualizarAsync(1, dto, "admin-1"))
            .ReturnsAsync(((Vehiculo?)null, "Otro error"));
        var res = await CreateCtrl().Actualizar(1, dto);
        res.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Actualizar_OK()
    {
        var dto = new ActualizarVehiculoDto { Matricula = "X", Tipo = TipoVehiculo.Furgoneta };
        _svc.Setup(s => s.ActualizarAsync(1, dto, "admin-1"))
            .ReturnsAsync((Sample(1), null));
        var res = await CreateCtrl().Actualizar(1, dto);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Actualizar_BadRequest_ModelState()
    {
        var ctrl = CreateCtrl();
        ctrl.ModelState.AddModelError("M", "x");
        var res = await ctrl.Actualizar(1, new ActualizarVehiculoDto());
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Desactivar_NotFound()
    {
        _svc.Setup(s => s.DesactivarAsync(99, "admin-1")).ReturnsAsync((false, "Vehículo no encontrado"));
        var res = await CreateCtrl().Desactivar(99);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Desactivar_Conflict()
    {
        _svc.Setup(s => s.DesactivarAsync(1, "admin-1")).ReturnsAsync((false, "Está asignado"));
        var res = await CreateCtrl().Desactivar(1);
        res.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Desactivar_OK()
    {
        _svc.Setup(s => s.DesactivarAsync(1, "admin-1")).ReturnsAsync((true, null));
        var res = await CreateCtrl().Desactivar(1);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Reactivar_NotFound()
    {
        _svc.Setup(s => s.ReactivarAsync(99, "admin-1")).ReturnsAsync((false, "Vehículo no encontrado"));
        var res = await CreateCtrl().Reactivar(99);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Reactivar_BadRequest()
    {
        _svc.Setup(s => s.ReactivarAsync(1, "admin-1")).ReturnsAsync((false, "Otro"));
        var res = await CreateCtrl().Reactivar(1);
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Reactivar_OK()
    {
        _svc.Setup(s => s.ReactivarAsync(1, "admin-1")).ReturnsAsync((true, null));
        var res = await CreateCtrl().Reactivar(1);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Asignar_NotFound_VehiculoNoEncontrado()
    {
        _svc.Setup(s => s.AsignarAsync(99, 5, "admin-1"))
            .ReturnsAsync(((Vehiculo?)null, "Vehículo no encontrado"));
        var res = await CreateCtrl().Asignar(99, new AsignarVehiculoDto { RepartidorId = 5 });
        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Asignar_NotFound_RepartidorNoEncontrado()
    {
        _svc.Setup(s => s.AsignarAsync(1, 999, "admin-1"))
            .ReturnsAsync(((Vehiculo?)null, "Repartidor no encontrado"));
        var res = await CreateCtrl().Asignar(1, new AsignarVehiculoDto { RepartidorId = 999 });
        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Asignar_BadRequest_OtroError()
    {
        _svc.Setup(s => s.AsignarAsync(1, 5, "admin-1"))
            .ReturnsAsync(((Vehiculo?)null, "Conflicto"));
        var res = await CreateCtrl().Asignar(1, new AsignarVehiculoDto { RepartidorId = 5 });
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Asignar_OK()
    {
        _svc.Setup(s => s.AsignarAsync(1, 5, "admin-1"))
            .ReturnsAsync((Sample(1), null));
        var res = await CreateCtrl().Asignar(1, new AsignarVehiculoDto { RepartidorId = 5 });
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Importar_OK()
    {
        _svc.Setup(s => s.ImportarDesdeRepartidoresAsync("admin-1"))
            .ReturnsAsync(new ImportarDesdeRepartidoresResultDto { Importados = 3 });
        var res = await CreateCtrl().Importar();
        var ok = res.Result as OkObjectResult;
        ok!.Value.Should().BeOfType<ImportarDesdeRepartidoresResultDto>()
          .Which.Importados.Should().Be(3);
    }
}
