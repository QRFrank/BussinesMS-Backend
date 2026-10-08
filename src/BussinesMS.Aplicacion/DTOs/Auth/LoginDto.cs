namespace BussinesMS.Aplicacion.DTOs.Auth;

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int? SistemaId { get; set; }
}
