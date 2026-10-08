namespace BussinesMS.Aplicacion.DTOs.Auth;

public class LoginResponseDto
{
    public UsuarioDto Usuario { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public string? RolNombre { get; set; }
    public int SistemaId { get; set; }
    public string? SistemaNombre { get; set; }
    public List<MenuArbolDto> Menus { get; set; } = [];
}
