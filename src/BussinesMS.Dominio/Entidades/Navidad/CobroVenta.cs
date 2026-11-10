using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Pago del cliente sobre una venta (en ruta, del vendedor). Anulación lógica
public class CobroVenta : EntidadBase
{
    public int VentaId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public MedioPagoNav Medio { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
    public bool Anulado { get; set; }

    // Devolución de la diferencia al cliente al editar la venta: Monto negativo, sale de la caja de la tienda. No se anula por separado
    public bool EsDevolucion { get; set; }

    // Abono del vendedor que originó este cobro (null si es un cobro directo)
    public int? AbonoVendedorId { get; set; }

    // Tienda en la que se recibió el dinero (caja de tienda). Null en abonos de ruta o si no se indicó. Sin FK: Almacen vive en AuthDB
    public int? AlmacenId { get; set; }

    public VentaNav? Venta { get; set; }
    public AbonoVendedor? AbonoVendedor { get; set; }
}
