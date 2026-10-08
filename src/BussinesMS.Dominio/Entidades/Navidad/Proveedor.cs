using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class Proveedor : EntidadBase
{
    public int TemporadaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool UsaCodigosCliente { get; set; }
    public string? Telefono { get; set; }
    public string? Observacion { get; set; }

    public Temporada? Temporada { get; set; }
    public ICollection<CodigoCliente> Codigos { get; set; } = new List<CodigoCliente>();
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
