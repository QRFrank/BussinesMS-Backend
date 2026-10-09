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
    // Ajuste 2: id de la compra al contado que generó este pago automático (null si es un pago normal).
    // Ese pago no se anula por separado: se anula la compra.
    public int? CompraId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearPagoProveedorNavDto
{
    public int ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public MedioPagoNav Medio { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
}
