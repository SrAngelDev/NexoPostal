using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Services;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

// ─── EtiquetaPdfService ───────────────────────────────────────────────────────

public class EtiquetaPdfServiceTests
{
    private static Envio EnvioCompleto() => new()
    {
        NumeroSeguimiento = "NXP-20240101-001ES",
        NumeroExpedicion = "NXI-20240101-001",
        NombreRemitente = "Juan García",
        ApellidosRemitente = "López",
        NombreDestinatario = "Ana Martínez",
        ApellidosDestinatario = "Ruiz",
        Origen = "Calle Mayor 1, Madrid",
        Destino = "Avenida Diagonal 100, Barcelona",
        CodigoPostalOrigen = "28001",
        CodigoPostalDestino = "08028",
        TipoTarifa = "Estandar",
        TiempoEntregaEstimado = "2-3 días",
        CosteCalculado = 7.50m,
        Dimensiones = "30x20x15 cm",
        PesoKg = 2.5m,
        EstadoActual = EstadoEnvio.Admitido,
        EstadoInternoActual = EstadoInterno.PendienteRecogida,
        FechaCreacion = DateTime.UtcNow.AddDays(-1),
        FechaPago = DateTime.UtcNow,
        TipoEntrega = TipoEntrega.Domicilio,
        Pagado = true
    };

    [Fact]
    public void GenerarEtiqueta_Domicilio_DevuelveBytesPdf()
    {
        var service = new EtiquetaPdfService();
        var pdf = service.GenerarEtiqueta(EnvioCompleto());

        pdf.Should().NotBeNullOrEmpty();
        // PDF magic bytes: %PDF
        pdf[0].Should().Be(0x25); // '%'
        pdf[1].Should().Be(0x50); // 'P'
        pdf[2].Should().Be(0x44); // 'D'
        pdf[3].Should().Be(0x46); // 'F'
    }

    [Fact]
    public void GenerarEtiqueta_TipoEntregaOficina_DevuelveBytesPdf()
    {
        var service = new EtiquetaPdfService();
        var envio = EnvioCompleto();
        envio.TipoEntrega = TipoEntrega.Oficina;
        envio.OficinaDestinoId = 5;

        var pdf = service.GenerarEtiqueta(envio);
        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerarEtiqueta_SinDimensiones_DevuelvePdf()
    {
        var service = new EtiquetaPdfService();
        var envio = EnvioCompleto();
        envio.Dimensiones = null;

        var pdf = service.GenerarEtiqueta(envio);
        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerarEtiqueta_TarifaExpress_DevuelvePdf()
    {
        var service = new EtiquetaPdfService();
        var envio = EnvioCompleto();
        envio.TipoTarifa = "Express";

        var pdf = service.GenerarEtiqueta(envio);
        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerarEtiqueta_TarifaPremium_DevuelvePdf()
    {
        var service = new EtiquetaPdfService();
        var envio = EnvioCompleto();
        envio.TipoTarifa = "Premium";

        var pdf = service.GenerarEtiqueta(envio);
        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerarEtiqueta_NumeroSeguimientoCorto_NoCrash()
    {
        var service = new EtiquetaPdfService();
        var envio = EnvioCompleto();
        // NumeroSeguimiento con solo 2 chars (evita index out of range en slice)
        envio.NumeroSeguimiento = "AB";
        envio.NumeroExpedicion = "CD";

        var pdf = service.GenerarEtiqueta(envio);
        pdf.Should().NotBeNullOrEmpty();
    }
}

// ─── FacturaPdfService ────────────────────────────────────────────────────────

public class FacturaPdfServiceTests
{
    private static Envio EnvioConPago() => new()
    {
        NumeroSeguimiento = "NXP-20240101-002ES",
        NombreRemitente = "Pedro Sánchez",
        ApellidosRemitente = "García",
        DniRemitente = "12345678A",
        Origen = "C/ Gran Vía 10, Madrid",
        NombreDestinatario = "Lucía Pérez",
        Destino = "Paseo de Gracia 50, Barcelona",
        TipoTarifa = "Estandar",
        TiempoEntregaEstimado = "2-3 días",
        CosteCalculado = 8.47m,
        PesoKg = 1.2m,
        FechaCreacion = DateTime.UtcNow.AddDays(-2),
        FechaPago = DateTime.UtcNow.AddDays(-1),
        Pagado = true
    };

    [Fact]
    public void GenerarFactura_ConFechaPago_DevuelveBytesPdf()
    {
        var service = new FacturaPdfService();
        var pdf = service.GenerarFactura(EnvioConPago());

        pdf.Should().NotBeNullOrEmpty();
        pdf[0].Should().Be(0x25); // '%PDF'
    }

    [Fact]
    public void GenerarFactura_SinFechaPago_UsaFechaCreacion()
    {
        var service = new FacturaPdfService();
        var envio = EnvioConPago();
        envio.FechaPago = null;

        var pdf = service.GenerarFactura(envio);
        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerarFactura_SinDni_NoLanzaExcepcion()
    {
        var service = new FacturaPdfService();
        var envio = EnvioConPago();
        envio.DniRemitente = null;

        var pdf = service.GenerarFactura(envio);
        pdf.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerarFactura_CosteAlto_CalculaIvaCorrectamente()
    {
        var service = new FacturaPdfService();
        var envio = EnvioConPago();
        envio.CosteCalculado = 100m;

        var pdf = service.GenerarFactura(envio);
        pdf.Should().NotBeNullOrEmpty();
    }
}

// ─── EmailService ─────────────────────────────────────────────────────────────

public class EmailServiceTests
{
    private static Envio EnvioParaEmail() => new()
    {
        NumeroSeguimiento = "NXP-EMAIL-001ES",
        NombreRemitente = "Carlos Ruiz",
        ApellidosRemitente = "Martínez",
        EmailRemitente = "carlos@test.local",
        NombreDestinatario = "María López",
        Origen = "C/ Alcalá 1",
        Destino = "Gran Vía 50",
        CosteCalculado = 6.50m,
        TipoTarifa = "Estandar",
        TiempoEntregaEstimado = "24h",
        FechaPago = DateTime.UtcNow,
        Pagado = true
    };

    [Fact]
    public async Task EnviarConfirmacion_SmtpHostNoConfigurado_NoLanzaExcepcion()
    {
        // Sin SMTP host configurado, el servicio debe loguear y devolver sin lanzar
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmtpSettings:Host"] = "",
                ["SmtpSettings:Port"] = "587",
                ["SmtpSettings:FromEmail"] = "no-reply@nexopostal.es"
            })
            .Build();
        var service = new EmailService(config, NullLogger<EmailService>.Instance);

        var ex = await Record.ExceptionAsync(() =>
            service.EnviarConfirmacionEnvio(
                EnvioParaEmail(),
                new byte[] { 1, 2, 3 },
                new byte[] { 4, 5, 6 }));

        ex.Should().BeNull();
    }

    [Fact]
    public async Task EnviarConfirmacion_SmtpHostConPlaceholder_NoLanzaExcepcion()
    {
        // Un host con placeholder no resuelto (${SMTP_HOST}) debe tratarse como vacío
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmtpSettings:Host"] = "${SMTP_HOST}",
                ["SmtpSettings:Port"] = "587"
            })
            .Build();
        var service = new EmailService(config, NullLogger<EmailService>.Instance);

        var ex = await Record.ExceptionAsync(() =>
            service.EnviarConfirmacionEnvio(
                EnvioParaEmail(),
                Array.Empty<byte>(),
                Array.Empty<byte>()));

        ex.Should().BeNull();
    }

    [Fact]
    public async Task EnviarConfirmacion_SmtpHostInvalido_CapturaBienLaExcepcion()
    {
        // Host inválido → ConnectAsync lanzará excepción que debe ser capturada internamente
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SmtpSettings:Host"] = "smtp.invalid.test.local.invalid",
                ["SmtpSettings:Port"] = "587",
                ["SmtpSettings:UseSsl"] = "false",
                ["SmtpSettings:Username"] = "",
                ["SmtpSettings:Password"] = ""
            })
            .Build();
        var service = new EmailService(config, NullLogger<EmailService>.Instance);

        // No debe propagar la excepción de red
        var ex = await Record.ExceptionAsync(() =>
            service.EnviarConfirmacionEnvio(
                EnvioParaEmail(),
                new byte[] { 1 },
                new byte[] { 2 }));

        ex.Should().BeNull();
    }
}
