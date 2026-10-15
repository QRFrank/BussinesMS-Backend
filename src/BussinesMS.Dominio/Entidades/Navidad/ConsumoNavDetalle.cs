using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class ConsumoNavDetalle : EntidadBase
{
    public int ConsumoId { get; set; }
    public int ProductoId { get; set; }
    public int Unidades { get; set; }

    public ConsumoNav? Consumo { get; set; }
    public Producto? Producto { get; set; }
}
