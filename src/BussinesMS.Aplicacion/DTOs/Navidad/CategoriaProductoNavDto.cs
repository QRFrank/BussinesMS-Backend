namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class CategoriaProductoNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearCategoriaProductoNavDto
{
    public string Nombre { get; set; } = string.Empty;
}

public class ActualizarCategoriaProductoNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
