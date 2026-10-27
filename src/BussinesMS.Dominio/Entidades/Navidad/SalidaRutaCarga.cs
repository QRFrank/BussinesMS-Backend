using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Carga de productos a una salida de ruta (una salida puede tener varias cargas)
public class SalidaRutaCarga : EntidadBase
{
    public int SalidaRutaId { get; set; }
    public DateTime Fecha { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    public bool Anulada { get; set; }

    public SalidaRuta? SalidaRuta { get; set; }
    public ICollection<SalidaRutaCargaDetalle> Detalles { get; set; } = new List<SalidaRutaCargaDetalle>();
}
