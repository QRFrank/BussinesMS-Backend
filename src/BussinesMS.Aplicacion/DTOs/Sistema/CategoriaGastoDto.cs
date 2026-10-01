namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class CategoriaGastoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearCategoriaGastoDto
{
    public string Nombre { get; set; } = string.Empty;
}

public class ActualizarCategoriaGastoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
