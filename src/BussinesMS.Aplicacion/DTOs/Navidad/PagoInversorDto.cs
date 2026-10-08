using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class PagoInversorFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? AporteCapitalId { get; set; }
    public int? InversorId { get; set; }
    public TipoPagoInversor? Tipo { get; set; }
    // null → solo activos; false → solo anulados; true → solo activos
    public bool? IsActive { get; set; }
}

public class PagoInversorDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int AporteCapitalId { get; set; }
    public int? InversorId { get; set; }
    public string InversorNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public TipoPagoInversor Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearPagoInversorDto
{
    public int AporteCapitalId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public TipoPagoInversor Tipo { get; set; }
    public string? Observacion { get; set; }
}
