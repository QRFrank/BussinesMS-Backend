using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Traslado de stock entre dos almacenes navideños. No se edita: se anula.
public class Traslado : EntidadBase
{
    public int TemporadaId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenOrigenId { get; set; }
    public int AlmacenDestinoId { get; set; }
    public DateTime Fecha { get; set; }
    public string? Motivo { get; set; }
    public bool Anulado { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }

    public Temporada? Temporada { get; set; }
    public ICollection<TrasladoDetalle> Detalles { get; set; } = new List<TrasladoDetalle>();
}
