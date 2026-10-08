using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class AporteCapitalFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? InversorId { get; set; }
    public bool? SoloCapitalPropio { get; set; }
}

public class AporteCapitalDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int? InversorId { get; set; }
    public string InversorNombre { get; set; } = string.Empty;
    public bool EsCapitalPropio { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public decimal PorcentajeComision { get; set; }
    public string? Observacion { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearAporteCapitalDto
{
    public int? InversorId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public decimal PorcentajeComision { get; set; }
    public string? Observacion { get; set; }
}

public class ActualizarAporteCapitalDto
{
    public int Id { get; set; }
    public int? InversorId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public decimal PorcentajeComision { get; set; }
    public string? Observacion { get; set; }
}

public class ResumenCapitalDto
{
    public int TemporadaId { get; set; }
    public string TemporadaNombre { get; set; } = string.Empty;
    public decimal CapitalTotal { get; set; }
    public decimal CapitalPropio { get; set; }
    public decimal CapitalInversores { get; set; }
    public decimal ComisionTotal { get; set; }
    public decimal ComisionPagada { get; set; }
    public decimal ComisionPorPagar { get; set; }
    public decimal CapitalDevuelto { get; set; }
    public decimal CapitalPorDevolver { get; set; }
    public List<ResumenAporteCapitalDto> Aportes { get; set; } = new();
}

public class ResumenAporteCapitalDto
{
    public int AporteCapitalId { get; set; }
    public int? InversorId { get; set; }
    public string InversorNombre { get; set; } = string.Empty;
    public bool EsCapitalPropio { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public decimal PorcentajeComision { get; set; }
    public decimal ComisionTotal { get; set; }
    public decimal ComisionPagada { get; set; }
    public decimal SaldoComision { get; set; }
    public decimal CapitalDevuelto { get; set; }
    public decimal SaldoCapital { get; set; }
}
