using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.Services;

public record TarifaConsultaInput(
    decimal Peso,
    decimal? Largo,
    decimal? Ancho,
    decimal? Alto,
    string? CodigoPostalOrigen,
    string? CodigoPostalDestino);

public record TarifaCalculoInput(
    decimal Peso,
    decimal? Largo,
    decimal? Ancho,
    decimal? Alto,
    string? CodigoPostalOrigen,
    string? CodigoPostalDestino,
    string? TipoTarifa);

public record TarifaOpcion(
    string Nombre,
    string Descripcion,
    string TiempoEntregaEstimado,
    int TiempoEstimadoDias,
    decimal PrecioBase,
    decimal Recargo,
    decimal Iva,
    decimal PrecioTotal);

public record TarifaConsultaResult(
    string Zona,
    decimal PesoReal,
    decimal PesoVolumetrico,
    decimal PesoFacturable,
    bool AplicaRecargo,
    decimal RecargoPorcentaje,
    IReadOnlyList<TarifaOpcion> Tarifas);

public record TarifaCalculoResult(
    string TipoTarifa,
    string Zona,
    string TiempoEntregaEstimado,
    int TiempoEstimadoDias,
    decimal PesoReal,
    decimal PesoVolumetrico,
    decimal PesoFacturable,
    decimal PrecioBase,
    decimal Recargo,
    decimal Iva,
    decimal PrecioTotal,
    bool AplicaRecargo,
    decimal RecargoPorcentaje);

public interface ITarifasService
{
    TarifaConsultaResult Consultar(TarifaConsultaInput input);
    TarifaCalculoResult Calcular(TarifaCalculoInput input);
    (decimal? Largo, decimal? Ancho, decimal? Alto) ParseDimensiones(string? dimensiones);
    void Invalidar();
}

public class TarifasService : ITarifasService
{
    private const decimal VolumetricDivisor = 6000m;
    private const decimal RecargoPorcentaje = 0.35m;
    private const decimal IvaPorcentaje = 0.21m;
    private static readonly decimal[] BandasPeso = [1m, 2m, 5m, 10m, 20m, 30m];

    // Fallback estático (defaults históricos) usado si la BD no tiene datos.
    private static readonly decimal[] DefaultLocalEstandar     = [4.50m, 5.25m, 6.95m,  9.95m, 14.95m, 19.95m];
    private static readonly decimal[] DefaultLocalPremium      = [6.50m, 7.75m, 10.50m, 14.95m, 21.95m, 29.95m];
    private static readonly decimal[] DefaultPeninsulaEstandar = [5.95m, 6.95m, 8.95m, 12.95m, 18.95m, 25.95m];
    private static readonly decimal[] DefaultPeninsulaPremium  = [8.95m, 10.50m, 13.95m, 19.95m, 28.95m, 38.95m];

    // Caché en memoria de los precios actuales (cargados desde BD on-demand).
    private static readonly object _cacheLock = new();
    private static decimal[]? _localEstandar;
    private static decimal[]? _localPremium;
    private static decimal[]? _peninsulaEstandar;
    private static decimal[]? _peninsulaPremium;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TarifasService> _logger;

    public TarifasService(IServiceScopeFactory scopeFactory, ILogger<TarifasService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Invalidar()
    {
        lock (_cacheLock)
        {
            _localEstandar = null;
            _localPremium = null;
            _peninsulaEstandar = null;
            _peninsulaPremium = null;
        }
    }

    private void EnsureLoaded()
    {
        if (_localEstandar is not null) return;
        lock (_cacheLock)
        {
            if (_localEstandar is not null) return;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CiudadanoDbContext>();
                var bandas = db.TarifasBandas.AsNoTracking().OrderBy(b => b.OrdenBanda).ToList();
                if (bandas.Count == 0)
                {
                    _localEstandar     = DefaultLocalEstandar;
                    _localPremium      = DefaultLocalPremium;
                    _peninsulaEstandar = DefaultPeninsulaEstandar;
                    _peninsulaPremium  = DefaultPeninsulaPremium;
                    return;
                }
                _localEstandar     = bandas.Where(b => b.Serie == TarifaSerie.LocalEstandar).OrderBy(b => b.OrdenBanda).Select(b => b.PrecioBase).ToArray();
                _localPremium      = bandas.Where(b => b.Serie == TarifaSerie.LocalPremium).OrderBy(b => b.OrdenBanda).Select(b => b.PrecioBase).ToArray();
                _peninsulaEstandar = bandas.Where(b => b.Serie == TarifaSerie.PeninsulaEstandar).OrderBy(b => b.OrdenBanda).Select(b => b.PrecioBase).ToArray();
                _peninsulaPremium  = bandas.Where(b => b.Serie == TarifaSerie.PeninsulaPremium).OrderBy(b => b.OrdenBanda).Select(b => b.PrecioBase).ToArray();
                if (_localEstandar.Length     != BandasPeso.Length) _localEstandar     = DefaultLocalEstandar;
                if (_localPremium.Length      != BandasPeso.Length) _localPremium      = DefaultLocalPremium;
                if (_peninsulaEstandar.Length != BandasPeso.Length) _peninsulaEstandar = DefaultPeninsulaEstandar;
                if (_peninsulaPremium.Length  != BandasPeso.Length) _peninsulaPremium  = DefaultPeninsulaPremium;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar tarifas desde BD; usando defaults");
                _localEstandar     = DefaultLocalEstandar;
                _localPremium      = DefaultLocalPremium;
                _peninsulaEstandar = DefaultPeninsulaEstandar;
                _peninsulaPremium  = DefaultPeninsulaPremium;
            }
        }
    }

    // Multiplicadores aplicados sobre precio Península para zonas especiales
    private static readonly Dictionary<string, decimal> MultiplicadorZona = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Local", 1.00m },       // Precio propio — se usa tabla Local directamente
        { "Península", 1.00m },
        { "Baleares", 1.15m },
        { "Ceuta/Melilla", 1.35m },
        { "Canarias", 1.45m }
    };

    private static readonly Dictionary<string, (string Estandar, int DiasEstandar, string Premium, int DiasPremium)> EtaPorZona
        = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Local",        ("24-48h",  2, "12-24h",  1) },
        { "Península",    ("48-72h",  3, "24-48h",  2) },
        { "Baleares",     ("72-96h",  4, "48-72h",  3) },
        { "Ceuta/Melilla",("4-6 días",6, "3-4 días",4) },
        { "Canarias",     ("4-7 días",7, "3-5 días",5) }
    };

    public TarifaConsultaResult Consultar(TarifaConsultaInput input)
    {
        EnsureLoaded();
        var baseCalc = BuildBaseCalculo(input);
        var tarifas = new List<TarifaOpcion>
        {
            CalcularOpcion(baseCalc, "Estandar"),
            CalcularOpcion(baseCalc, "Premium")
        };

        return new TarifaConsultaResult(
            baseCalc.Zona,
            baseCalc.PesoReal,
            baseCalc.PesoVolumetrico,
            baseCalc.PesoFacturable,
            baseCalc.AplicaRecargo,
            baseCalc.RecargoPorcentaje,
            tarifas);
    }

    public TarifaCalculoResult Calcular(TarifaCalculoInput input)
    {
        EnsureLoaded();
        var baseCalc = BuildBaseCalculo(new TarifaConsultaInput(
            input.Peso,
            input.Largo,
            input.Ancho,
            input.Alto,
            input.CodigoPostalOrigen,
            input.CodigoPostalDestino));

        var tipoTarifa = NormalizarTipoTarifa(input.TipoTarifa);
        var opcion = CalcularOpcion(baseCalc, tipoTarifa);

        return new TarifaCalculoResult(
            tipoTarifa,
            baseCalc.Zona,
            opcion.TiempoEntregaEstimado,
            opcion.TiempoEstimadoDias,
            baseCalc.PesoReal,
            baseCalc.PesoVolumetrico,
            baseCalc.PesoFacturable,
            opcion.PrecioBase,
            opcion.Recargo,
            opcion.Iva,
            opcion.PrecioTotal,
            baseCalc.AplicaRecargo,
            baseCalc.RecargoPorcentaje);
    }

    public (decimal? Largo, decimal? Ancho, decimal? Alto) ParseDimensiones(string? dimensiones)
    {
        if (string.IsNullOrWhiteSpace(dimensiones))
        {
            return (null, null, null);
        }

        var matches = Regex.Matches(dimensiones, @"(\d+[\.,]?\d*)");
        if (matches.Count < 3)
        {
            return (null, null, null);
        }

        return (
            ParseDecimal(matches[0].Value),
            ParseDecimal(matches[1].Value),
            ParseDecimal(matches[2].Value)
        );
    }

    private static decimal ParseDecimal(string value)
    {
        var normalized = value.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;
    }

    private static string NormalizarTipoTarifa(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return "Estandar";
        }

        return tipo.Trim().Equals("premium", StringComparison.OrdinalIgnoreCase) ? "Premium" : "Estandar";
    }

    private static int GetBandIndex(decimal peso)
    {
        for (var i = 0; i < BandasPeso.Length; i++)
        {
            if (peso <= BandasPeso[i])
            {
                return i;
            }
        }

        return BandasPeso.Length - 1;
    }

    private static BaseCalculo BuildBaseCalculo(TarifaConsultaInput input)
    {
        var pesoReal = input.Peso <= 0 ? 0.1m : input.Peso;
        var zona = ResolverZona(input.CodigoPostalOrigen, input.CodigoPostalDestino);

        var (pesoVolumetrico, aplicaRecargo) = CalcularPesoVolumetricoYRecargo(
            input.Largo,
            input.Ancho,
            input.Alto);

        var pesoFacturable = Math.Max(pesoReal, pesoVolumetrico);

        return new BaseCalculo(
            zona,
            pesoReal,
            pesoVolumetrico,
            pesoFacturable,
            aplicaRecargo,
            aplicaRecargo ? RecargoPorcentaje : 0m);
    }

    private static (decimal PesoVolumetrico, bool AplicaRecargo) CalcularPesoVolumetricoYRecargo(
        decimal? largo,
        decimal? ancho,
        decimal? alto)
    {
        if (!largo.HasValue || !ancho.HasValue || !alto.HasValue)
        {
            return (0m, false);
        }

        var volumen = largo.Value * ancho.Value * alto.Value;
        var pesoVolumetrico = volumen / VolumetricDivisor;

        var sumaDimensiones = largo.Value + ancho.Value + alto.Value;
        var ladoMayor = Math.Max(largo.Value, Math.Max(ancho.Value, alto.Value));

        var aplicaRecargo = sumaDimensiones > 210m || ladoMayor > 170m;

        return (pesoVolumetrico, aplicaRecargo);
    }

    private static string ResolverZona(string? codigoPostalOrigen, string? codigoPostalDestino)
    {
        var origen = (codigoPostalOrigen ?? string.Empty).Trim();
        var destino = (codigoPostalDestino ?? string.Empty).Trim();

        // Zonas especiales tienen prioridad sobre la local
        if (EsCanarias(origen) || EsCanarias(destino)) return "Canarias";
        if (EsCeutaOMelilla(origen) || EsCeutaOMelilla(destino)) return "Ceuta/Melilla";
        if (EsBaleares(origen) || EsBaleares(destino)) return "Baleares";

        // Zona Local: misma provincia (2 primeros dígitos del CP coinciden)
        if (origen.Length >= 2 && destino.Length >= 2 && origen[..2] == destino[..2])
            return "Local";

        return "Península";
    }

    private static bool EsCanarias(string codigoPostal)
    {
        return codigoPostal.StartsWith("35", StringComparison.Ordinal) ||
               codigoPostal.StartsWith("38", StringComparison.Ordinal);
    }

    private static bool EsBaleares(string codigoPostal)
    {
        return codigoPostal.StartsWith("07", StringComparison.Ordinal);
    }

    private static bool EsCeutaOMelilla(string codigoPostal)
    {
        return codigoPostal.StartsWith("51", StringComparison.Ordinal) ||
               codigoPostal.StartsWith("52", StringComparison.Ordinal);
    }

    private TarifaOpcion CalcularOpcion(BaseCalculo baseCalc, string tipoTarifa)
    {
        var index = GetBandIndex(baseCalc.PesoFacturable);
        var zona = baseCalc.Zona;
        var esPremium = tipoTarifa.Equals("Premium", StringComparison.OrdinalIgnoreCase);

        // Zona Local usa su propia tabla de precios; el resto usa Península × multiplicador
        decimal precioBase;
        if (zona.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            precioBase = esPremium ? _localPremium![index] : _localEstandar![index];
        }
        else
        {
            var tablaBase = esPremium ? _peninsulaPremium![index] : _peninsulaEstandar![index];
            precioBase = RedondearMoneda(tablaBase * MultiplicadorZona[zona]);
        }

        // Recargo por dimensiones extra (suma > 210 cm o lado > 170 cm)
        var recargo = baseCalc.AplicaRecargo ? RedondearMoneda(precioBase * baseCalc.RecargoPorcentaje) : 0m;

        // Subtotal sin IVA
        var subtotal = RedondearMoneda(precioBase + recargo);

        // IVA 21%
        var iva = RedondearMoneda(subtotal * IvaPorcentaje);

        // Precio total con IVA
        var total = RedondearMoneda(subtotal + iva);

        var descripcion = esPremium
            ? "Entrega premium prioritaria"
            : "Entrega estándar económica";

        var eta = EtaPorZona[zona];
        var tiempoEntrega = esPremium ? eta.Premium : eta.Estandar;
        var dias = esPremium ? eta.DiasPremium : eta.DiasEstandar;

        return new TarifaOpcion(
            esPremium ? "Premium" : "Estandar",
            descripcion,
            tiempoEntrega,
            dias,
            precioBase,
            recargo,
            iva,
            total);
    }

    private static decimal RedondearMoneda(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private sealed record BaseCalculo(
        string Zona,
        decimal PesoReal,
        decimal PesoVolumetrico,
        decimal PesoFacturable,
        bool AplicaRecargo,
        decimal RecargoPorcentaje);
}
