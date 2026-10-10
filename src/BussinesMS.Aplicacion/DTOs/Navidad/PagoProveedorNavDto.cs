using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class PagoProveedorNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    public MedioPagoNav? Medio { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // null/true → solo activos; false → solo anulados
    public bool? IsActive { get; set; }
}

public class PagoProveedorNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoTitular { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public MedioPagoNav Medio { get; set; }
    public string MedioNombre { get; set; } = string.Empty;
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
    // Id de la compra que generó este pago automático (contado o pago inicial); null si es un pago normal.
    // Se edita y se anula como cualquier pago.
    public int? CompraId { get; set; }
    // N° de nota de esa compra (para mostrar)
    public string? CompraNroNota { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActualizarPagoProveedorNavDto
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public MedioPagoNav Medio { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
}

public class CrearPagoProveedorNavDto
{
    public int ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    // Opcional: paga esa compra (sin código de cliente; monto <= saldo de la compra)
    public int? CompraId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public MedioPagoNav Medio { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
}
