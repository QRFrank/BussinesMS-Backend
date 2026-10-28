using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Pago que el cliente le hace al vendedor sobre una venta personal. Anulación lógica
public class CobroClienteRuta : EntidadBase
{
    public int VentaClienteRutaId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public MedioPagoNav? Medio { get; set; }
    public bool Anulado { get; set; }

    public VentaClienteRuta? VentaClienteRuta { get; set; }
}
