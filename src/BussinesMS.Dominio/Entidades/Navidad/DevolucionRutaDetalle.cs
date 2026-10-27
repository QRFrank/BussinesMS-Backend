using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class DevolucionRutaDetalle : EntidadBase
{
    public int DevolucionId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenDestinoId { get; set; }
    public int ProductoId { get; set; }
    public int Unidades { get; set; }

    public DevolucionRuta? Devolucion { get; set; }
    public Producto? Producto { get; set; }
}
