using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

// Registro personal y privado de ventas del vendedor de ruta a sus clientes

// ---------- Lista ----------

public class MisVentasRutaFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? ClienteId { get; set; }
    // Por Fecha de la venta (solo la fecha)
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    // Solo ventas no anuladas con saldo > 0
    public bool SoloConSaldo { get; set; }
    public bool IncluirAnuladas { get; set; }
}

public class VentaClienteRutaListaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int? SalidaRutaId { get; set; }
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
    public bool Anulada { get; set; }
    public int CantidadLineas { get; set; }
}

// ---------- Detalle ----------

public class VentaClienteRutaDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public DateTime Fecha { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int? SalidaRutaId { get; set; }
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
    public bool Anulada { get; set; }
    public string? Observacion { get; set; }
    public List<VentaClienteRutaLineaDetalleDto> Detalles { get; set; } = new();
    public List<VentaClienteRutaCobroDto> Cobros { get; set; } = new();
    // Solo al registrar: productos cuyo total registrado en la salida abierta supera lo que tiene en poder (nunca bloquea)
    public List<string> Avisos { get; set; } = new();
}

public class VentaClienteRutaLineaDetalleDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class VentaClienteRutaCobroDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    // 1 Transferencia | 2 Efectivo
    public int? Medio { get; set; }
    public string? MedioNombre { get; set; }
    public bool Anulado { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---------- Requests ----------

public class RegistrarVentaClienteRutaDto
{
    public int ClienteId { get; set; }
    // Omitida = su salida Abierta (si no hay, queda sin salida)
    public int? SalidaRutaId { get; set; }
    // Omitida = hoy (Bolivia)
    public DateTime? Fecha { get; set; }
    public string? Observacion { get; set; }
    // Cobrado al registrar (0 = todo a crédito)
    public decimal PagadoAhora { get; set; }
    // Obligatorio si PagadoAhora > 0
    public MedioPagoNav? Medio { get; set; }
    public List<VentaClienteRutaLineaDto> Detalles { get; set; } = new();
}

public class VentaClienteRutaLineaDto
{
    public int ProductoId { get; set; }
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class RegistrarCobroClienteRutaDto
{
    public decimal Monto { get; set; }
    // Omitida = hoy (Bolivia)
    public DateTime? Fecha { get; set; }
    public MedioPagoNav Medio { get; set; }
}

// ---------- Me deben ----------

public class MisVentasMeDebenDto
{
    public decimal TotalGeneral { get; set; }
    public List<MisVentasMeDebenClienteDto> Clientes { get; set; } = new();
}

public class MisVentasMeDebenClienteDto
{
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal SaldoTotal { get; set; }
    public int VentasPendientes { get; set; }
    public List<MisVentasMeDebenVentaDto> Ventas { get; set; } = new();
}

public class MisVentasMeDebenVentaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
}

// ---------- Ayuda del cierre ----------

public class MisVentasTotalSalidaDto
{
    public int SalidaRutaId { get; set; }
    public decimal TotalRegistrado { get; set; }
    public int CantidadVentas { get; set; }
}
