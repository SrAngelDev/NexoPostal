using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Nexopostal.Ciudadano.Models;
using QRCoder;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Genera etiquetas de envío profesionales en PDF usando QuestPDF.
/// Formato: 10×15 cm (100×150 mm) — estándar de etiqueta de paquetería.
///
/// La etiqueta incluye DOS códigos de barras con funciones distintas:
///
///   1. CÓDIGO PÚBLICO (QR + Code128 superior):
///      Contiene el NumeroSeguimiento (NX...ES).
///      Es el que el CLIENTE escanea para consultar el estado público
///      en la web de NexoPostal. Muestra información simplificada.
///
///   2. CÓDIGO INTERNO (Code128 inferior):
///      Contiene el NumeroExpedicion (NXI-...).
///      Es el que los OPERARIOS y REPARTIDORES escanean en la intranet
///      y driver-app. Muestra información detallada y permite gestionar
///      el estado interno del envío.
///
/// Diseño inspirado en las etiquetas de Correos, adaptado a NexoPostal.
/// </summary>
public interface IEtiquetaPdfService
{
    byte[] GenerarEtiqueta(Envio envio);
}

public class EtiquetaPdfService : IEtiquetaPdfService
{
    // Colores corporativos NexoPostal
    private static readonly string PrimaryColor = "#1A237E";   // Indigo oscuro
    private static readonly string AccentColor = "#303F9F";    // Indigo medio
    private static readonly string LightBg = "#F5F5F5";        // Gris claro
    private static readonly string BorderColor = "#BDBDBD";    // Gris borde

    public EtiquetaPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerarEtiqueta(Envio envio)
    {
        // Generar códigos de barras y QR
        var barcodePublico = GenerarCodigoBarrasCode128(envio.NumeroSeguimiento);
        var barcodeInterno = GenerarCodigoBarrasCode128(envio.NumeroExpedicion);
        var qrPublico = GenerarCodigoQR(envio.NumeroSeguimiento);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                // 100 × 150 mm — estándar de etiqueta de envío
                page.Size(100, 150, Unit.Millimetre);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Arial"));

                page.Content().Border(0.5f).BorderColor(BorderColor).Column(col =>
                {
                    // ═══════════════════════════════════════════
                    // CABECERA: Logo + Tipo de servicio + Expedición + QR
                    // ═══════════════════════════════════════════
                    col.Item().Background(PrimaryColor).Padding(3, Unit.Millimetre).Row(row =>
                    {
                        // Logo NexoPostal
                        row.RelativeItem(3).Column(c =>
                        {
                            c.Item().Text("NexoPostal").Bold().FontSize(14).FontColor(Colors.White);
                            c.Item().Text("Operador Logístico Nacional").FontSize(5.5f).FontColor("#B0BEC5");
                        });

                        // Tipo de servicio (badge)
                        row.RelativeItem(3).AlignCenter().AlignMiddle().Column(c =>
                        {
                            c.Item().AlignCenter()
                                .Background(Colors.White)
                                .Padding(2, Unit.Millimetre)
                                .MinWidth(50, Unit.Point)
                                .Column(badge =>
                                {
                                    badge.Item().AlignCenter().Text(ObtenerNombreServicio(envio.TipoTarifa))
                                        .Bold().FontSize(9).FontColor(PrimaryColor);
                                    badge.Item().AlignCenter().Text(envio.TiempoEntregaEstimado)
                                        .FontSize(6.5f).FontColor(AccentColor);
                                });
                        });

                        // Código QR público
                        row.RelativeItem(2).AlignRight().AlignMiddle()
                            .Width(48, Unit.Point).Height(48, Unit.Point)
                            .Background(Colors.White).Padding(2).Image(qrPublico);
                    });

                    // ═══════════════════════════════════════════
                    // NÚMERO DE SEGUIMIENTO PÚBLICO
                    // ═══════════════════════════════════════════
                    col.Item().Background(LightBg)
                        .PaddingHorizontal(3, Unit.Millimetre)
                        .PaddingVertical(1.5f, Unit.Millimetre)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("SEGUIMIENTO").FontSize(5).FontColor("#757575").Bold();
                                c.Item().Text(envio.NumeroSeguimiento).Bold().FontSize(10).FontColor(Colors.Black).LetterSpacing(0.5f);
                            });
                        });

                    // ═══════════════════════════════════════════
                    // REMITENTE
                    // ═══════════════════════════════════════════
                    col.Item().PaddingHorizontal(3, Unit.Millimetre)
                        .PaddingTop(2, Unit.Millimetre)
                        .PaddingBottom(1.5f, Unit.Millimetre)
                        .Column(rem =>
                        {
                            rem.Item().Row(r =>
                            {
                                r.AutoItem().Width(3, Unit.Point).Height(24, Unit.Point)
                                    .Background(AccentColor);
                                r.RelativeItem().PaddingLeft(2, Unit.Millimetre).Column(c =>
                                {
                                    c.Item().Text("REMITENTE").Bold().FontSize(5.5f).FontColor(AccentColor);
                                    c.Item().Text($"{envio.NombreRemitente} {envio.ApellidosRemitente}")
                                        .Bold().FontSize(7.5f);
                                    c.Item().Text(envio.Origen).FontSize(6.5f).FontColor("#616161");
                                    c.Item().Text($"CP: {envio.CodigoPostalOrigen}    Tel: {envio.TelefonoRemitente}")
                                        .FontSize(6).FontColor("#9E9E9E");
                                });
                            });
                        });

                    // Separador
                    col.Item().PaddingHorizontal(3, Unit.Millimetre)
                        .LineHorizontal(0.5f).LineColor(BorderColor);

                    // ═══════════════════════════════════════════
                    // DESTINATARIO (sección principal — más grande)
                    // ═══════════════════════════════════════════
                    col.Item().PaddingHorizontal(3, Unit.Millimetre)
                        .PaddingVertical(2, Unit.Millimetre)
                        .Border(1.5f).BorderColor(PrimaryColor)
                        .Padding(2.5f, Unit.Millimetre)
                        .Column(dest =>
                        {
                            dest.Item().Text("DESTINATARIO").Bold().FontSize(6).FontColor(PrimaryColor);
                            dest.Item().PaddingTop(1, Unit.Millimetre)
                                .Text($"{envio.NombreDestinatario} {envio.ApellidosDestinatario}")
                                .Bold().FontSize(10);
                            dest.Item().Text(envio.Destino).FontSize(8);
                            dest.Item().PaddingTop(1, Unit.Millimetre).Row(r =>
                            {
                                r.RelativeItem().Column(cpCol =>
                                {
                                    cpCol.Item().Text("CP DESTINO").FontSize(5).FontColor("#757575");
                                    cpCol.Item().Text(envio.CodigoPostalDestino).Bold().FontSize(14).FontColor(PrimaryColor);
                                });
                                if (!string.IsNullOrEmpty(envio.TelefonoDestinatario))
                                {
                                    r.RelativeItem().AlignRight().Column(telCol =>
                                    {
                                        telCol.Item().AlignRight().Text("TELÉFONO").FontSize(5).FontColor("#757575");
                                        telCol.Item().AlignRight().Text(envio.TelefonoDestinatario).FontSize(8);
                                    });
                                }
                            });
                        });

                    // ═══════════════════════════════════════════
                    // DATOS DEL PAQUETE
                    // ═══════════════════════════════════════════
                    col.Item().PaddingHorizontal(3, Unit.Millimetre)
                        .PaddingTop(1.5f, Unit.Millimetre)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PESO").FontSize(5).FontColor("#757575");
                                c.Item().Text($"{envio.PesoKg} kg").Bold().FontSize(8);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("DIMENSIONES").FontSize(5).FontColor("#757575");
                                c.Item().Text($"{envio.Dimensiones} cm").FontSize(7);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("BULTO").FontSize(5).FontColor("#757575");
                                c.Item().Text("1 / 1").Bold().FontSize(8);
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text("FECHA").FontSize(5).FontColor("#757575");
                                c.Item().AlignRight().Text(envio.FechaCreacion.ToString("dd/MM/yyyy")).FontSize(7);
                            });
                        });

                    // ═══════════════════════════════════════════
                    // CÓDIGO DE BARRAS PÚBLICO (NumeroSeguimiento)
                    // ═══════════════════════════════════════════
                    col.Item().PaddingHorizontal(3, Unit.Millimetre)
                        .PaddingTop(2, Unit.Millimetre)
                        .Column(bc =>
                        {
                            bc.Item().AlignCenter().Height(28, Unit.Point).Image(barcodePublico);
                            bc.Item().AlignCenter().Text(envio.NumeroSeguimiento)
                                .FontSize(7).Bold().LetterSpacing(0.8f);
                        });

                    // ═══════════════════════════════════════════
                    // SEPARADOR CON LÍNEA DE CORTE VISUAL
                    // ═══════════════════════════════════════════
                    col.Item().PaddingVertical(1.5f, Unit.Millimetre)
                        .PaddingHorizontal(1, Unit.Millimetre)
                        .LineHorizontal(0.8f).LineColor("#E0E0E0");

                    // ═══════════════════════════════════════════
                    // CÓDIGO DE BARRAS INTERNO (NumeroExpedicion)
                    // Solo visible para operarios/repartidores
                    // ═══════════════════════════════════════════
                    col.Item().PaddingHorizontal(3, Unit.Millimetre)
                        .PaddingBottom(2, Unit.Millimetre)
                        .Background(LightBg)
                        .Padding(2, Unit.Millimetre)
                        .Column(bc =>
                        {
                            bc.Item().Text("EXPEDICIÓN INTERNA").FontSize(5).FontColor("#9E9E9E").Bold();
                            bc.Item().PaddingTop(1, Unit.Millimetre).AlignCenter()
                                .Height(25, Unit.Point).Image(barcodeInterno);
                            bc.Item().AlignCenter().Text(envio.NumeroExpedicion)
                                .FontSize(7.5f).Bold().LetterSpacing(1f).FontColor("#424242");
                        });
                });
            });
        }).GeneratePdf();
    }

    /// <summary>
    /// Nombre de servicio formateado para la etiqueta
    /// </summary>
    private static string ObtenerNombreServicio(string tipoTarifa) => tipoTarifa?.ToLower() switch
    {
        "urgente" => "URGENTE",
        "estandar" or "estándar" => "ESTÁNDAR",
        _ => tipoTarifa?.ToUpper() ?? "ESTÁNDAR"
    };

    #region Generación de Código de Barras Code128

    /// <summary>
    /// Genera un código de barras Code128B como imagen BMP (sin dependencias nativas)
    /// </summary>
    private static byte[] GenerarCodigoBarrasCode128(string texto)
    {
        var anchos = CodificarCode128B(texto);
        return RenderizarCodigoBarrasBmp(anchos, barHeight: 60, moduleWidth: 2);
    }

    /// <summary>
    /// Codifica texto en patrón Code128B (lista de anchos de barras/espacios)
    /// </summary>
    private static List<int> CodificarCode128B(string texto)
    {
        int[][] patrones =
        [
            [2,1,2,2,2,2],[2,2,2,1,2,2],[2,2,2,2,2,1],[1,2,1,2,2,3],[1,2,1,3,2,2],
            [1,3,1,2,2,2],[1,2,2,2,1,3],[1,2,2,3,1,2],[1,3,2,2,1,2],[2,2,1,2,1,3],
            [2,2,1,3,1,2],[2,3,1,2,1,2],[1,1,2,2,3,2],[1,2,2,1,3,2],[1,2,2,2,3,1],
            [1,1,3,2,2,2],[1,2,3,1,2,2],[1,2,3,2,2,1],[2,2,3,2,1,1],[2,2,1,1,3,2],
            [2,2,1,2,3,1],[2,1,3,2,1,2],[2,2,3,1,1,2],[3,1,2,1,3,1],[3,1,1,2,2,2],
            [3,2,1,1,2,2],[3,2,1,2,2,1],[3,1,2,2,1,2],[3,2,2,1,1,2],[3,2,2,2,1,1],
            [2,1,2,1,2,3],[2,1,2,3,2,1],[2,3,2,1,2,1],[1,1,1,3,2,3],[1,3,1,1,2,3],
            [1,3,1,3,2,1],[1,1,2,3,1,3],[1,3,2,1,1,3],[1,3,2,3,1,1],[2,1,1,3,1,3],
            [2,3,1,1,1,3],[2,3,1,3,1,1],[1,1,2,1,3,3],[1,1,2,3,3,1],[1,3,2,1,3,1],
            [1,1,3,1,2,3],[1,1,3,3,2,1],[1,3,3,1,2,1],[3,1,3,1,2,1],[2,1,1,3,3,1],
            [2,3,1,1,3,1],[2,1,3,1,1,3],[2,1,3,3,1,1],[2,1,3,1,3,1],[3,1,1,1,2,3],
            [3,1,1,3,2,1],[3,3,1,1,2,1],[3,1,2,1,1,3],[3,1,2,3,1,1],[3,3,2,1,1,1],
            [3,1,4,1,1,1],[2,2,1,4,1,1],[4,3,1,1,1,1],[1,1,1,2,2,4],[1,1,1,4,2,2],
            [1,2,1,1,2,4],[1,2,1,4,2,1],[1,4,1,1,2,2],[1,4,1,2,2,1],[1,1,2,2,1,4],
            [1,1,2,4,1,2],[1,2,2,1,1,4],[1,2,2,4,1,1],[1,4,2,1,1,2],[1,4,2,2,1,1],
            [2,4,1,2,1,1],[2,2,1,1,1,4],[4,1,3,1,1,1],[2,4,1,1,1,2],[1,3,4,1,1,1],
            [1,1,1,2,4,2],[1,2,1,1,4,2],[1,2,1,2,4,1],[1,1,4,2,1,2],[1,2,4,1,1,2],
            [1,2,4,2,1,1],[4,1,1,2,1,2],[4,2,1,1,1,2],[4,2,1,2,1,1],[2,1,2,1,4,1],
            [2,1,4,1,2,1],[4,1,2,1,2,1],[1,1,1,1,4,3],[1,1,1,3,4,1],[1,3,1,1,4,1],
            [1,1,4,1,1,3],[1,1,4,3,1,1],[4,1,1,1,1,3],[4,1,1,3,1,1],[1,1,3,1,4,1],
            [1,1,4,1,3,1],[3,1,1,1,4,1],[4,1,1,1,3,1],[2,1,1,4,1,2],[2,1,1,2,1,4],
            [2,1,1,2,3,2],[2,3,3,1,1,1,2]
        ];

        const int START_B = 104;
        const int STOP = 106;

        var valores = new List<int> { START_B };
        foreach (char c in texto)
        {
            int val = c - 32;
            if (val < 0 || val > 94) val = 0;
            valores.Add(val);
        }

        int checksum = valores[0];
        for (int i = 1; i < valores.Count; i++)
            checksum += i * valores[i];
        checksum %= 103;
        valores.Add(checksum);
        valores.Add(STOP);

        var anchos = new List<int>();
        foreach (var val in valores)
        {
            foreach (var w in patrones[val])
                anchos.Add(w);
        }

        return anchos;
    }

    /// <summary>
    /// Renderiza código de barras como BMP sin dependencias nativas
    /// </summary>
    private static byte[] RenderizarCodigoBarrasBmp(List<int> anchos, int barHeight, int moduleWidth)
    {
        const int quietZone = 10;
        int totalModules = quietZone * 2;
        foreach (var w in anchos) totalModules += w;

        int width = totalModules * moduleWidth;
        int height = barHeight;

        var rowPixels = new bool[width];
        int x = quietZone * moduleWidth;
        bool isBar = true;

        foreach (var w in anchos)
        {
            int barWidth = w * moduleWidth;
            if (isBar)
            {
                for (int i = 0; i < barWidth && (x + i) < width; i++)
                    rowPixels[x + i] = true;
            }
            x += barWidth;
            isBar = !isBar;
        }

        int bytesPerPixelRow = width * 3;
        int rowPadding = (4 - (bytesPerPixelRow % 4)) % 4;
        int rowStride = bytesPerPixelRow + rowPadding;
        int pixelDataSize = rowStride * height;
        int fileSize = 54 + pixelDataSize;

        using var ms = new MemoryStream(fileSize);
        using var bw = new BinaryWriter(ms);

        bw.Write((byte)'B'); bw.Write((byte)'M');
        bw.Write(fileSize);
        bw.Write(0);
        bw.Write(54);

        bw.Write(40);
        bw.Write(width);
        bw.Write(height);
        bw.Write((short)1);
        bw.Write((short)24);
        bw.Write(0);
        bw.Write(pixelDataSize);
        bw.Write(3780);
        bw.Write(3780);
        bw.Write(0);
        bw.Write(0);

        var rowBytes = new byte[rowStride];
        for (int px = 0; px < width; px++)
        {
            int offset = px * 3;
            if (rowPixels[px])
            {
                rowBytes[offset] = 0;
                rowBytes[offset + 1] = 0;
                rowBytes[offset + 2] = 0;
            }
            else
            {
                rowBytes[offset] = 255;
                rowBytes[offset + 1] = 255;
                rowBytes[offset + 2] = 255;
            }
        }

        for (int row = 0; row < height; row++)
            bw.Write(rowBytes);

        return ms.ToArray();
    }

    #endregion

    #region Generación de Código QR

    /// <summary>
    /// Genera un código QR como imagen PNG usando QRCoder
    /// </summary>
    private static byte[] GenerarCodigoQR(string texto)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(texto, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrData);
        return qrCode.GetGraphic(8, [0, 0, 0], [255, 255, 255]);
    }

    #endregion
}
