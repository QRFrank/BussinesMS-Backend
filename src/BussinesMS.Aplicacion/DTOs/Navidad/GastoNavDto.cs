using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class GastoNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? CategoriaId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

public class GastoNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GastoNavPagedResultDto : PagedResultDto<GastoNavDto>
{
    // Suma de Monto de todo el resultado filtrado (no solo de la página)
    public decimal TotalMonto { get; set; }
}

public class CrearGastoNavDto
{
    public int CategoriaId { get; set; }
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}

public class ActualizarGastoNavDto
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}
