using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Genera facturas de envío en PDF usando QuestPDF
/// Formato A4 con datos fiscales, desglose de servicio e IVA
/// </summary>
public interface IFacturaPdfService
{
    byte[] GenerarFactura(Envio envio);
}

public class FacturaPdfService : IFacturaPdfService
{
    public FacturaPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerarFactura(Envio envio)
    {
        var fechaFactura = envio.FechaPago ?? envio.FechaCreacion;
        var numeroFactura = $"FV-{fechaFactura:yyyyMMdd}-{envio.NumeroSeguimiento[2..]}";
        var baseImponible = Math.Round(envio.CosteCalculado / 1.21m, 2);
        var iva = envio.CosteCalculado - baseImponible;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                // === CABECERA ===
                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("NexoPostal").Bold().FontSize(22).FontColor(Colors.Indigo.Darken4);
                            c.Item().Text("Operador Logístico Nacional").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Text("NexoPostal S.A.").FontSize(8);
                            c.Item().Text("CIF: A-12345678").FontSize(8);
                            c.Item().Text("Calle de la Logística, 1 — 28001 Madrid").FontSize(8);
                            c.Item().Text("info@nexopostal.es — www.nexopostal.es").FontSize(8);
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("FACTURA").Bold().FontSize(18).FontColor(Colors.Indigo.Darken4);
                            c.Item().PaddingTop(5).Text($"Nº: {numeroFactura}").FontSize(9);
                            c.Item().Text($"Fecha: {fechaFactura:dd/MM/yyyy}").FontSize(9);
                            c.Item().Text($"Ref. envío: {envio.NumeroSeguimiento}").FontSize(9);
                        });
                    });

                    header.Item().PaddingVertical(10)
                        .LineHorizontal(2).LineColor(Colors.Indigo.Darken4);
                });

                // === CONTENIDO ===
                page.Content().PaddingVertical(10).Column(content =>
                {
                    // --- DATOS DEL CLIENTE ---
                    content.Item().Background(Colors.Grey.Lighten4)
                        .Padding(12).Column(cliente =>
                    {
                        cliente.Item().Text("DATOS DEL CLIENTE").Bold().FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                        cliente.Item().PaddingTop(4)
                            .Text($"{envio.NombreRemitente} {envio.ApellidosRemitente}").Bold();
                        if (!string.IsNullOrEmpty(envio.DniRemitente))
                            cliente.Item().Text($"DNI/NIF: {envio.DniRemitente}");
                        cliente.Item().Text(envio.Origen);
                        cliente.Item().Text($"CP: {envio.CodigoPostalOrigen}");
                        cliente.Item().Text($"Email: {envio.EmailRemitente}");
                        cliente.Item().Text($"Teléfono: {envio.TelefonoRemitente}");
                    });

                    content.Item().PaddingVertical(15);

                    // --- TABLA DE SERVICIOS ---
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4); // Descripción
                            cols.RelativeColumn(1); // Cantidad
                            cols.RelativeColumn(1.5f); // Precio unitario
                            cols.RelativeColumn(1.5f); // Total
                        });

                        // Cabecera de tabla
                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Indigo.Darken4).Padding(6)
                                .Text("Descripción").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Background(Colors.Indigo.Darken4).Padding(6)
                                .Text("Cant.").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Background(Colors.Indigo.Darken4).Padding(6).AlignRight()
                                .Text("P. Unitario").FontColor(Colors.White).Bold().FontSize(9);
                            h.Cell().Background(Colors.Indigo.Darken4).Padding(6).AlignRight()
                                .Text("Total").FontColor(Colors.White).Bold().FontSize(9);
                        });

                        // Fila de servicio
                        var descripcion = $"Servicio de envío {envio.TipoTarifa} ({envio.TiempoEntregaEstimado})\n" +
                                          $"Peso: {envio.PesoKg} kg — Dimensiones: {envio.Dimensiones} cm\n" +
                                          $"Origen: {envio.Origen}\n" +
                                          $"Destino: {envio.Destino}";

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                            .Text(descripcion).FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                            .Text("1").FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight()
                            .Text($"{baseImponible:F2} €").FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignRight()
                            .Text($"{baseImponible:F2} €").FontSize(9);
                    });

                    content.Item().PaddingVertical(10);

                    // --- TOTALES ---
                    content.Item().AlignRight().Width(200).Column(totales =>
                    {
                        totales.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Base Imponible:").FontSize(9);
                            r.RelativeItem().AlignRight().Text($"{baseImponible:F2} €").FontSize(9);
                        });
                        totales.Item().Row(r =>
                        {
                            r.RelativeItem().Text("IVA (21%):").FontSize(9);
                            r.RelativeItem().AlignRight().Text($"{iva:F2} €").FontSize(9);
                        });
                        totales.Item().PaddingTop(5)
                            .LineHorizontal(1).LineColor(Colors.Indigo.Darken4);
                        totales.Item().PaddingTop(5).Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL:").Bold().FontSize(12).FontColor(Colors.Indigo.Darken4);
                            r.RelativeItem().AlignRight().Text($"{envio.CosteCalculado:F2} €")
                                .Bold().FontSize(12).FontColor(Colors.Indigo.Darken4);
                        });
                    });

                    content.Item().PaddingVertical(15);

                    // --- ESTADO DEL PAGO ---
                    content.Item().Background(Colors.Green.Lighten4).Padding(10).Row(r =>
                    {
                        r.AutoItem().PaddingRight(8).Text("✓").Bold().FontSize(14).FontColor(Colors.Green.Darken3);
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("PAGADO").Bold().FontColor(Colors.Green.Darken3);
                            c.Item().Text($"Pago recibido el {fechaFactura:dd/MM/yyyy HH:mm}").FontSize(8)
                                .FontColor(Colors.Green.Darken1);
                        });
                    });
                });

                // === PIE DE PÁGINA ===
                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    footer.Item().PaddingTop(5).Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("NexoPostal S.A. — CIF: A-12345678 — ").FontSize(7).FontColor(Colors.Grey.Medium);
                            t.Span("Inscrita en el Registro Mercantil de Madrid").FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                        r.AutoItem().Text(t =>
                        {
                            t.Span("Página ").FontSize(7);
                            t.CurrentPageNumber().FontSize(7);
                            t.Span(" de ").FontSize(7);
                            t.TotalPages().FontSize(7);
                        });
                    });
                });
            });
        }).GeneratePdf();
    }
}
