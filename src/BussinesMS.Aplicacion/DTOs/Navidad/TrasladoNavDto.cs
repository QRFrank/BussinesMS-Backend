using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class TrasladoNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    // Origen O destino
    public int? AlmacenId { get; set; }
    public int? AlmacenOrigenId { get; set; }
    public int? AlmacenDestinoId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // null → todos; true → solo anulados; false → solo vigentes
    public bool? Anulados { get; set; }
}

public class TrasladoNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int AlmacenOrigenId { get; set; }
    public string AlmacenOrigenNombre { get; set; } = string.Empty;
    public int AlmacenDestinoId { get; set; }
    public string AlmacenDestinoNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Motivo { get; set; }
    public bool Anulado { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public int CantidadProductos { get; set; }
    public int TotalUnidades { get; set; }
    // Vacío en la lista
    public List<TrasladoDetalleNavDto> Detalles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TrasladoDetalleNavDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int CantidadUnidades { get; set; }
}

public class CrearTrasladoNavDto
{
    public int AlmacenOrigenId { get; set; }
    public int AlmacenDestinoId { get; set; }
    // Opcional: por defecto hoy (Bolivia); no puede ser futura
    public DateTime? Fecha { get; set; }
    public string? Motivo { get; set; }
    public List<CrearTrasladoDetalleNavDto> Detalles { get; set; } = new();
}

public class CrearTrasladoDetalleNavDto
{
    public int ProductoId { get; set; }
    public int CantidadUnidades { get; set; }
}
