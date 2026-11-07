namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public record ComprobanteLineaNav(string Producto, int Unidades, decimal PrecioUnitario, decimal Subtotal);

public record ComprobantePagoNav(string Medio, decimal Monto);

/// <summary>Datos de una venta para el comprobante (sin precios de compra ni costos). Fecha en hora de Bolivia.</summary>
public class ComprobanteVentaDatos
{
    public int VentaId { get; set; }
    public int NroVenta { get; set; }
    public bool EsReserva { get; set; }
    /// <summary>C7: venta de "Mis ventas" del vendedor de ruta (el vendedor se rotula "Vendedor:" y la fecha es solo el día).</summary>
    public bool EsVentaRuta { get; set; }
    public bool Anulada { get; set; }
    public string Tienda { get; set; } = string.Empty;
    public DateTime FechaLocal { get; set; }
    public string? Cliente { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public List<ComprobanteLineaNav> Lineas { get; set; } = new();
    public decimal Total { get; set; }
    public decimal Descuento { get; set; }
    public decimal Neto { get; set; }
    public decimal Cobrado { get; set; }
    public decimal Saldo { get; set; }
    public List<ComprobantePagoNav> Pagos { get; set; } = new();
}

/// <summary>Arma los datos del comprobante desde la venta, sin depender del usuario logueado (lo usa la bandeja en segundo plano).</summary>
public interface IComprobanteVentaDatosService
{
    /// <summary>Null si la venta no existe o está dada de baja.</summary>
    Task<ComprobanteVentaDatos?> ObtenerAsync(int ventaId);
}

/// <summary>C7: arma los datos del comprobante desde una venta de "Mis ventas" (VentaClienteRuta), sin depender del usuario logueado.</summary>
public interface IComprobanteMisVentasDatosService
{
    /// <summary>Null si la venta no existe o está dada de baja.</summary>
    Task<ComprobanteVentaDatos?> ObtenerAsync(int ventaId);
}

/// <summary>Genera el PDF tipo ticket del comprobante (implementado en Infraestructura con QuestPDF).</summary>
public interface IComprobantePdfService
{
    byte[] Generar(ComprobanteVentaDatos datos, string firma);
}
