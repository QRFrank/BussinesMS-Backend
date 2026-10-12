using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class KardexNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? AlmacenId { get; set; }
    public int? ProductoId { get; set; }
    // Días en hora de Bolivia, inclusive
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // TipoMovimientoNav
    public int? Tipo { get; set; }
    // Por defecto cronológico ascendente
    public bool Descendente { get; set; }
}

public class KardexNavDto
{
    public int Id { get; set; }
    // Hora de Bolivia
    public DateTime Fecha { get; set; }
    public int Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public string ReferenciaTipo { get; set; } = string.Empty;
    public int ReferenciaId { get; set; }
    // Texto legible ("Recepción #5 · Factura X")
    public string Referencia { get; set; } = string.Empty;
    public int Entrada { get; set; }
    public int Salida { get; set; }
    // Saldo real acumulado del producto en el almacén tras este movimiento (independiente de los filtros)
    public int Saldo { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}
