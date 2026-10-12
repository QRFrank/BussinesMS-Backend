using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class ConteoInventarioDetalle : EntidadBase
{
    public int ConteoId { get; set; }
    public int ProductoId { get; set; }
    // Stock del sistema al momento de guardar (antes del ajuste)
    public int StockSistema { get; set; }
    public int Contado { get; set; }
    // Contado - StockSistema
    public int Diferencia { get; set; }
    public string? Observacion { get; set; }

    public ConteoInventario? Conteo { get; set; }
    public Producto? Producto { get; set; }
}
