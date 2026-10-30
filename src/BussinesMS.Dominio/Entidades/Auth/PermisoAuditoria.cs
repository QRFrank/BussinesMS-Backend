namespace BussinesMS.Dominio.Entidades.Auth;

// Registro de decisiones de permisos que habrían sido (o fueron) un 403 (fase 2 de permisos)
public class PermisoAuditoria
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public int UsuarioId { get; set; }
    public string? Username { get; set; }
    public int? SistemaId { get; set; }
    public string Metodo { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string? Menus { get; set; }
    public string? FlagRequerido { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
}
