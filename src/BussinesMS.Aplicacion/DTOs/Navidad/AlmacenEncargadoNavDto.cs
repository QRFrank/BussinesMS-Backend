namespace BussinesMS.Aplicacion.DTOs.Navidad;

// Usuario encargado (o candidato a encargado) de un almacén navideño
public class EncargadoNavDto
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

// Almacén navideño con sus encargados de la temporada y el indicador para el usuario actual
public class AlmacenNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool EsTienda { get; set; }
    public string? Direccion { get; set; }
    public int SistemaId { get; set; }
    public bool IsActive { get; set; }
    // true si el usuario actual puede operar en el almacén: siempre true salvo que sea EncargadoAlmacen sin ese almacén asignado
    public bool ACargo { get; set; }
    public List<EncargadoNavDto> Encargados { get; set; } = new();
}

public class AlmacenNavFiltroDto
{
    public int? TemporadaId { get; set; }
    // true: solo almacenes activos (default: todos)
    public bool? SoloActivos { get; set; }
}

public class AsignarEncargadosNavDto
{
    public List<int> UsuarioIds { get; set; } = new();
}
