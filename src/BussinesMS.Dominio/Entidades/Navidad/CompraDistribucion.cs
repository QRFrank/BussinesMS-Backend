using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class CompraDistribucion : EntidadBase
{
    public int CompraDetalleId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    public int CantidadUnidades { get; set; }

    public CompraDetalle? CompraDetalle { get; set; }
}
