using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class PagoCompraDto
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public decimal Monto { get; set; }
    public decimal MontoCaja { get; set; }
    public decimal MontoExterno { get; set; }
    public DateTime FechaPago { get; set; }
    public int? SesionCajaId { get; set; }
    public int PagadoPorUsuarioId { get; set; }
    public string? Observacion { get; set; }
}

public class CrearPagoCompraDto
{
    public int CompraId { get; set; }
    public decimal MontoCaja { get; set; }
    public decimal MontoExterno { get; set; }
    // Obligatorio si MontoCaja > 0; no debe venir si MontoCaja == 0.
    public int? SesionCajaId { get; set; }
    public string? Observacion { get; set; }
}

public class ActualizarPagoCompraDto
{
    public int Id { get; set; }
    public decimal MontoCaja { get; set; }
    public decimal MontoExterno { get; set; }
    public string? Observacion { get; set; }
}

public class PagoCompraFiltroDto : GenericPaginationQueryDto
{
    public int? CompraId { get; set; }
    public int? ProveedorId { get; set; }
    public int? SesionCajaId { get; set; }
    // "caja" → MontoCaja > 0 && MontoExterno == 0; "externo" → MontoCaja == 0;
    // "mixto" → MontoCaja > 0 && MontoExterno > 0
    public string? Origen { get; set; }
    public int? PagadoPorUsuarioId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // null → solo activos (por defecto); false → solo anulados
    public bool? IsActive { get; set; }
}

public class PagoCompraListDto
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public string? CompraNumeroFactura { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public decimal TotalCompra { get; set; }
    public decimal SaldoPendiente { get; set; }
    public decimal Monto { get; set; }
    public decimal MontoCaja { get; set; }
    public decimal MontoExterno { get; set; }
    public DateTime FechaPago { get; set; }
    public int? SesionCajaId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public int PagadoPorUsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public string? Observacion { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
