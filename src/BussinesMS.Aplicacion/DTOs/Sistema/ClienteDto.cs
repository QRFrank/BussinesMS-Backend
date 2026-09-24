namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class ClienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? NumeroCarnet { get; set; }
    public string? Telefono { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearClienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? NumeroCarnet { get; set; }
    public string? Telefono { get; set; }
}

public class ActualizarClienteDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? NumeroCarnet { get; set; }
    public string? Telefono { get; set; }
    public bool IsActive { get; set; }
}
