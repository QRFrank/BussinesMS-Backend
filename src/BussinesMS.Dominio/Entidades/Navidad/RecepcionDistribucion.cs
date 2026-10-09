using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class RecepcionDistribucion : EntidadBase
{
    public int RecepcionDetalleId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    public int CantidadUnidades { get; set; }

    public RecepcionDetalle? RecepcionDetalle { get; set; }
}
