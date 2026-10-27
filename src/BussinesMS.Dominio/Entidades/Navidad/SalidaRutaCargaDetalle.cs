using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class SalidaRutaCargaDetalle : EntidadBase
{
    public int CargaId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenOrigenId { get; set; }
    public int ProductoId { get; set; }
    public int Unidades { get; set; }

    public SalidaRutaCarga? Carga { get; set; }
    public Producto? Producto { get; set; }
}
