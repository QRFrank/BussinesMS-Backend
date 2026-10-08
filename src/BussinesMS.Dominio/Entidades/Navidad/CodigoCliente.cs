using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class CodigoCliente : EntidadBase
{
    // Copiado del proveedor
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Titular { get; set; } = string.Empty;

    public Temporada? Temporada { get; set; }
    public Proveedor? Proveedor { get; set; }
}
