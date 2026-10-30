namespace BussinesMS.Aplicacion.DTOs.Auth;

// Desde/Hasta se comparan contra Fecha (UTC).
public class PermisoAuditoriaFiltroDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? UsuarioId { get; set; }
    public string? Modo { get; set; }
}

public class PermisoAuditoriaResumenDto
{
    public int UsuarioId { get; set; }
    public string? Username { get; set; }
    public int? SistemaId { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? Menus { get; set; }
    public string? FlagRequerido { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime PrimeraVezUtc { get; set; }
    public DateTime UltimaVezUtc { get; set; }
    public string? RutaEjemplo { get; set; }
}
