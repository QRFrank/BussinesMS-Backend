namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class InversorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearInversorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
}

public class ActualizarInversorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public bool IsActive { get; set; }
}
