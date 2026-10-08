using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Auth;

public class Almacen : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public bool EsTienda { get; set; } = false;
    public string? Direccion { get; set; }
    public int SistemaId { get; set; } = 1;
    public Sistema? Sistema { get; set; }
}