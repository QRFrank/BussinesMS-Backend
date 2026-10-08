using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Auth;

public class Usuario : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int SistemaIdDefault { get; set; } = 1;

    public int RolId { get; set; }
    public Rol? Rol { get; set; }

    public ICollection<UsuarioMenu> UsuarioMenus { get; set; } = new List<UsuarioMenu>();
    public ICollection<UsuarioSistema> UsuarioSistemas { get; set; } = new List<UsuarioSistema>();
}