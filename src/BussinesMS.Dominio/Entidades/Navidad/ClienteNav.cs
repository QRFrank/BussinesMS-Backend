using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Global: no pertenece a una temporada
public class ClienteNav : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
}
