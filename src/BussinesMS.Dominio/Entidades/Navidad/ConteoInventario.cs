using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Conteo de inventario de una tienda: apertura o cierre del día. Un conteo por (temporada, almacén, fecha, tipo).
public class ConteoInventario : EntidadBase
{
    public int TemporadaId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    public DateTime Fecha { get; set; }
    public TipoConteoInventario Tipo { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    public string? Observacion { get; set; }

    public Temporada? Temporada { get; set; }
    public ICollection<ConteoInventarioDetalle> Detalles { get; set; } = new List<ConteoInventarioDetalle>();
}
