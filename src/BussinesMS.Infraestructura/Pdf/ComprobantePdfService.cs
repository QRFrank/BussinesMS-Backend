using System.Globalization;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Servicios.Navidad;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BussinesMS.Infraestructura.Pdf;

/// <summary>
/// Comprobante de venta tipo ticket (80 mm de ancho, alto continuo) con QuestPDF. Diseño simple, pensado para cambiarlo después.
/// Nunca incluye precios de compra ni costos.
/// </summary>
public class ComprobantePdfService : IComprobantePdfService
{
    static ComprobantePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static readonly NumberFormatInfo Num = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = ".", NumberGroupSizes = new[] { 3 } };
    private static string N(decimal v) => v.ToString("N2", Num);

    public byte[] Generar(ComprobanteVentaDatos d, string firma)
    {
        var titulo = d.EsReserva ? "Comprobante de reserva" : "Comprobante de venta";

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.ContinuousSize(80, Unit.Millimetre);
                page.Margin(4, Unit.Millimetre);
                page.DefaultTextStyle(t => t.FontSize(8.5f));

                page.Content().Column(col =>
                {
                    col.Spacing(2);

                    col.Item().AlignCenter().Text(firma).Bold().FontSize(12);
                    col.Item().AlignCenter().Text($"{titulo} N° {d.NroVenta}").Bold();
                    if (!string.IsNullOrWhiteSpace(d.Tienda))
                        col.Item().AlignCenter().Text(d.Tienda);
                    col.Item().AlignCenter().Text(MensajesAvisosNav.Fecha(d.FechaLocal));
                    if (!string.IsNullOrWhiteSpace(d.Cliente))
                        col.Item().AlignCenter().Text($"Cliente: {d.Cliente}");

                    Separador(col);

                    foreach (var l in d.Lineas)
                    {
                        col.Item().Text(l.Producto).Bold();
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"{l.Unidades} x Bs {N(l.PrecioUnitario)}");
                            r.AutoItem().AlignRight().Text(N(l.Subtotal));
                        });
                    }

                    Separador(col);

                    Fila(col, "Total", "Bs " + N(d.Total));
                    if (d.Descuento > 0) Fila(col, "Descuento", "- Bs " + N(d.Descuento));
                    Fila(col, "A pagar", "Bs " + N(d.Neto), negrita: true);

                    foreach (var p in d.Pagos)
                        Fila(col, $"Pagado ({p.Medio.ToLowerInvariant()})", "Bs " + N(p.Monto));
                    if (d.Pagos.Count == 0 && d.Cobrado > 0)
                        Fila(col, "Pagado", "Bs " + N(d.Cobrado));
                    Fila(col, "Saldo", "Bs " + N(d.Saldo), negrita: d.Saldo > 0);

                    Separador(col);

                    if (!string.IsNullOrWhiteSpace(d.Vendedor))
                        col.Item().AlignCenter().Text($"Te atendió: {d.Vendedor}");
                    col.Item().AlignCenter().Text("¡Gracias por tu compra y feliz Navidad!");
                    col.Item().AlignCenter().Text(firma).FontSize(7.5f);
                });
            });
        }).GeneratePdf();
    }

    private static void Separador(ColumnDescriptor col) =>
        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

    private static void Fila(ColumnDescriptor col, string etiqueta, string valor, bool negrita = false) =>
        col.Item().Row(r =>
        {
            var e = r.RelativeItem().Text(etiqueta);
            var v = r.AutoItem().AlignRight().Text(valor);
            if (negrita) { e.Bold(); v.Bold(); }
        });
}
