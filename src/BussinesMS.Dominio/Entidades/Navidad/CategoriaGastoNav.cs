using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Categoría global (no depende de la temporada)
public class CategoriaGastoNav : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
}
