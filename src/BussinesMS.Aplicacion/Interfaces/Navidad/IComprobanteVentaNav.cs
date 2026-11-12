namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public record ComprobanteLineaNav(string Producto, int Unidades, decimal PrecioUnitario, decimal Subtotal, string? Equivalencia = null);

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

/// <summary>Producto entregado: Cantidad ya formateada con su equivalencia en empaques ("24 u (2 javas)").</summary>
public record ComprobanteEntregaLineaNav(string Producto, int Unidades, string Cantidad);

/// <summary>Datos de una entrega de reserva para su comprobante (sin precios, saldo ni pendientes). Fecha en hora de Bolivia.</summary>
public class ComprobanteEntregaDatos
{
    public int EntregaId { get; set; }
    public int NroVenta { get; set; }
    /// <summary>La entrega o su reserva están anuladas: no se envía.</summary>
    public bool Anulada { get; set; }
    public string Almacen { get; set; } = string.Empty;
    public DateTime FechaLocal { get; set; }
    public string? Cliente { get; set; }
    public string Entrego { get; set; } = string.Empty;
    public List<ComprobanteEntregaLineaNav> Lineas { get; set; } = new();
}

/// <summary>Arma los datos del comprobante de una entrega de reserva, sin depender del usuario logueado (lo usa la bandeja).</summary>
public interface IComprobanteEntregaDatosService
{
    /// <summary>Null si la entrega o su reserva no existen o están dadas de baja.</summary>
    Task<ComprobanteEntregaDatos?> ObtenerAsync(int entregaId);
}

/// <summary>Genera el PDF tipo ticket del comprobante (implementado en Infraestructura con QuestPDF).</summary>
public interface IComprobantePdfService
{
    byte[] Generar(ComprobanteVentaDatos datos, string firma);
    byte[] GenerarEntrega(ComprobanteEntregaDatos datos, string firma);
}
