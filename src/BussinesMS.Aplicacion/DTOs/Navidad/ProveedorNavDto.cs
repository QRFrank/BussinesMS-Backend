using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class ProveedorNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public bool? UsaCodigosCliente { get; set; }
}

public class ProveedorNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool UsaCodigosCliente { get; set; }
    public string? Telefono { get; set; }
    public string? Observacion { get; set; }
    // Conteos de registros activos
    public int CantidadCodigos { get; set; }
    public int CantidadProductos { get; set; }
    // Solo se llena en GET por id; en la lista va vacía
    public List<CodigoClienteDto> Codigos { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearProveedorNavDto
{
    public string Nombre { get; set; } = string.Empty;
    public bool UsaCodigosCliente { get; set; }
    public string? Telefono { get; set; }
    public string? Observacion { get; set; }
}

public class ActualizarProveedorNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool UsaCodigosCliente { get; set; }
    public string? Telefono { get; set; }
    public string? Observacion { get; set; }
}
