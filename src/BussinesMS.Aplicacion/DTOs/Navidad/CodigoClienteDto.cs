using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class CodigoClienteFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? ProveedorId { get; set; }
}

public class CodigoClienteDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Titular { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearCodigoClienteDto
{
    public int ProveedorId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titular { get; set; } = string.Empty;
}

// No permite cambiar de proveedor
public class ActualizarCodigoClienteDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titular { get; set; } = string.Empty;
}
