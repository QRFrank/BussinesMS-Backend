using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class GastoOperativo : EntidadBase
{
    public int SesionCajaId { get; set; }
    public int CategoriaGastoId { get; set; }
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
    public bool EsPagoProveedor { get; set; } = false;
    public int? PagoCompraId { get; set; }

    public SesionCaja? SesionCaja { get; set; }
    public CategoriaGasto? CategoriaGasto { get; set; }
    public PagoCompra? PagoCompra { get; set; }
}
