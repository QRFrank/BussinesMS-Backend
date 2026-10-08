namespace BussinesMS.Aplicacion.DTOs.Auth;

public class AlmacenDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool EsTienda { get; set; }
    public string? Direccion { get; set; }
    public int SistemaId { get; set; }
    public bool IsActive { get; set; }
}

public class CrearAlmacenDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool EsTienda { get; set; }
    public string? Direccion { get; set; }
    public int? SistemaId { get; set; }
}

public class ActualizarAlmacenDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool EsTienda { get; set; }
    public string? Direccion { get; set; }
    public bool IsActive { get; set; }
}
