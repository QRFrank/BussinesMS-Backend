namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class MovimientoInventarioDto
{
    public int Id { get; set; }
    public int LoteId { get; set; }
    public int VarianteId { get; set; }
    public string? VarianteNombre { get; set; }
    public int? AlmacenOrigenId { get; set; }
    public int? AlmacenDestinoId { get; set; }
    public int TipoMovimiento { get; set; }
    public int CantidadUnidades { get; set; }
    public int SaldoResultante { get; set; }
    public int? ReferenciaId { get; set; }
    public string? Observacion { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public int UsuarioId { get; set; }
}

public class MovimientoInventarioFiltroDto
{
    public int? LoteId { get; set; }
    public int? VarianteId { get; set; }
    public int? AlmacenOrigenId { get; set; }
    public int? AlmacenDestinoId { get; set; }
    public int? TipoMovimiento { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}
