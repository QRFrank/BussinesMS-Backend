using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Auth;

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SistemaIdDefault { get; set; }
    public int? RolId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int SistemaIdDefault { get; set; } = 1;
    public int RolId { get; set; }
    public List<MenuPermisoSimpleDto>? Menus { get; set; }
}

public class ActualizarUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int SistemaIdDefault { get; set; }
    public string Username { get; set; } = string.Empty;
    public int RolId { get; set; }
}

public class CambiarPasswordUsuarioDto
{
    public string Password { get; set; } = string.Empty;
}

public class UsuarioFiltroDto : GenericPaginationQueryDto
{
    // null → todos (activos e inactivos); true → solo activos; false → solo inactivos
    public bool? IsActive { get; set; }
}

public class UsuarioConMenusDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SistemaIdDefault { get; set; }
    public int RolId { get; set; }
    public string? RolNombre { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MenuArbolDto> Menus { get; set; } = [];
}
