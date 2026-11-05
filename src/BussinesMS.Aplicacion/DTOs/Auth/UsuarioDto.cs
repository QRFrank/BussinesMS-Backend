using System.Text.Json.Serialization;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Auth;

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    [OcultoSinAdmin] // GET /Usuarios: lookup liviano para no-admin (permisos fase 2)
    public string? Email { get; set; }
    // Enmascarado (+591 7123****); el número completo solo viaja en el detalle (GET /Usuarios/{id}) para el admin
    [OcultoSinAdmin]
    public string? Telefono { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SistemaIdDefault { get; set; }
    [OcultoSinAdmin]
    public int? RolId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Solo lo llena el listado (GET /api/Usuarios); en el resto de respuestas queda null y no se serializa
    [OcultoSinAdmin]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<int>? SistemaIds { get; set; }
}

public class CrearUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    // Opcional. Se guarda normalizado (+591XXXXXXXX); 400 TELEFONO_INVALIDO si no se puede normalizar
    public string? Telefono { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int SistemaIdDefault { get; set; } = 1;
    public int RolId { get; set; }
    public List<MenuPermisoSimpleDto>? Menus { get; set; }
    public List<int>? SistemaIds { get; set; }
    // Permiso "Puede hacer recepciones" del sistema Navideño. null = no asignarlo (queda en false)
    public bool? PuedeHacerRecepciones { get; set; }
}

public class ActualizarUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    // null = no cambiarlo; vacío = borrarlo; con valor = normalizar y guardar (400 TELEFONO_INVALIDO si no es válido)
    public string? Telefono { get; set; }
    public int SistemaIdDefault { get; set; }
    public string Username { get; set; } = string.Empty;
    public int RolId { get; set; }
    // Permiso "Ver precios de compra" del sistema Navideño. null = no cambiarlo. Solo lo puede cambiar el admin
    public bool? VerPreciosCompra { get; set; }
    // Permiso "Puede hacer recepciones" del sistema Navideño. null = no cambiarlo. Solo lo puede cambiar el admin
    public bool? PuedeHacerRecepciones { get; set; }
    public List<int>? SistemaIds { get; set; }
}

public class CambiarPasswordUsuarioDto
{
    public string Password { get; set; } = string.Empty;
}

public class UsuarioFiltroDto : GenericPaginationQueryDto
{
    // null → todos (activos e inactivos); true → solo activos; false → solo inactivos
    public bool? IsActive { get; set; }

    // null → todos los sistemas; con valor → solo usuarios con acceso a ese sistema
    public int? SistemaId { get; set; }
}

public class UsuarioConMenusDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    // Normalizado completo (+591XXXXXXXX): es el detalle para editar; el endpoint es de admin
    public string? Telefono { get; set; }
    public string Username { get; set; } = string.Empty;
    public int SistemaIdDefault { get; set; }
    public List<int> SistemaIds { get; set; } = [];
    public int RolId { get; set; }
    public string? RolNombre { get; set; }
    // Permiso "Ver precios de compra" en el sistema Navideño (el admin lo tiene siempre, aparte de esta casilla)
    public bool VerPreciosCompra { get; set; }
    // Permiso "Puede hacer recepciones" en el sistema Navideño (el admin lo tiene siempre, aparte de esta casilla)
    public bool PuedeHacerRecepciones { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MenuArbolDto> Menus { get; set; } = [];
}
