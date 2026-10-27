using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Devolución de productos de una salida de ruta al almacén
public class DevolucionRuta : EntidadBase
{
    public int SalidaRutaId { get; set; }
    public DateTime Fecha { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    // Registrada por el admin desde "Editar venta de ruta" con la salida ya cerrada
    public bool EsTardia { get; set; }
    public bool Anulada { get; set; }
    public string? Observacion { get; set; }

    public SalidaRuta? SalidaRuta { get; set; }
    public ICollection<DevolucionRutaDetalle> Detalles { get; set; } = new List<DevolucionRutaDetalle>();
}
