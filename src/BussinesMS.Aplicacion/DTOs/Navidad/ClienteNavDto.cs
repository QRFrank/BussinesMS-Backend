namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class ClienteNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearClienteNavDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
}

public class ActualizarClienteNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool IsActive { get; set; }
}
