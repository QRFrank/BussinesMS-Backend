namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class CategoriaGastoNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearCategoriaGastoNavDto
{
    public string Nombre { get; set; } = string.Empty;
}

public class ActualizarCategoriaGastoNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
