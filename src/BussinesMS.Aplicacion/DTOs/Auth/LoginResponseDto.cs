namespace BussinesMS.Aplicacion.DTOs.Auth;

public class LoginResponseDto
{
    public UsuarioDto Usuario { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public string? RolNombre { get; set; }
    public int SistemaId { get; set; }
    public string? SistemaNombre { get; set; }
    public List<MenuArbolDto> Menus { get; set; } = [];

    // Permiso "Ver precios de compra" del sistema Navideño (true siempre para el rol Admin)
    public bool VerPreciosCompra { get; set; }

    // Permiso "Puede hacer recepciones" del sistema Navideño (true siempre para el rol Admin)
    public bool PuedeHacerRecepciones { get; set; }
}
