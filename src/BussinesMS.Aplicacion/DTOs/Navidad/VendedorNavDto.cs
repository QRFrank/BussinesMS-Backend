using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class VendedorFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? Tipo { get; set; }
}

public class VendedorNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombreCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public TipoVendedor Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public decimal? SueldoMensual { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearVendedorNavDto
{
    public int UsuarioId { get; set; }
    public TipoVendedor Tipo { get; set; }
    public decimal? SueldoMensual { get; set; }
}

public class ActualizarVendedorNavDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public TipoVendedor Tipo { get; set; }
    public decimal? SueldoMensual { get; set; }
}

public class UsuarioDisponibleNavDto
{
    public int UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
