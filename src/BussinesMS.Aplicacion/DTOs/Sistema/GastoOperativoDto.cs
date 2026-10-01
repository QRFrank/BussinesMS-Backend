using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class GastoOperativoFiltroDto : GenericPaginationQueryDto
{
    public int? SesionCajaId { get; set; }
    // "caja" → MontoCaja > 0 && MontoExterno == 0; "externo" → MontoCaja == 0;
    // "mixto" → MontoCaja > 0 && MontoExterno > 0
    public string? Origen { get; set; }
    public int? CategoriaGastoId { get; set; }
    public int? AlmacenId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // null → solo activos (por defecto); false → solo anulados
    public bool? IsActive { get; set; }
}

public class GastoOperativoListDto
{
    public int Id { get; set; }
    public int? SesionCajaId { get; set; }
    public int CategoriaGastoId { get; set; }
    public string CategoriaGastoNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal MontoCaja { get; set; }
    public decimal MontoExterno { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaGasto { get; set; }
    public int? AlmacenId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public int CreatedByUsuarioId { get; set; }
    public string? UsuarioNombre { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearGastoOperativoDto
{
    // Obligatorio si MontoCaja > 0; no debe venir si MontoCaja == 0.
    public int? SesionCajaId { get; set; }
    public int CategoriaGastoId { get; set; }
    public decimal MontoCaja { get; set; }
    public decimal MontoExterno { get; set; }
    public string? Descripcion { get; set; }
    // Hora local Bolivia. Solo se usa en gastos externos (sin SesionCajaId).
    public DateTime? FechaGasto { get; set; }
    // Solo se usa en gastos externos (sin SesionCajaId).
    public int? AlmacenId { get; set; }
}

public class ActualizarGastoOperativoDto
{
    public int Id { get; set; }
    public int CategoriaGastoId { get; set; }
    public decimal MontoCaja { get; set; }
    public decimal MontoExterno { get; set; }
    public string? Descripcion { get; set; }
    // Hora local Bolivia. Solo se usa en gastos externos (sin SesionCajaId).
    public DateTime? FechaGasto { get; set; }
}
