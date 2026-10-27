using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Stock apartado por una reserva en un almacén concreto. Se elimina de forma lógica (IsActive = false) al desbloquear,
// consumirse con una entrega o anularse la reserva; al desbloquear solo una parte baja Unidades.
// Invariante: Σ Unidades vigentes de una línea <= pendiente de entrega de la línea (Unidades - UnidadesEntregadas).
public class ReservaBloqueo : EntidadBase
{
    public int VentaId { get; set; }
    public int VentaDetalleId { get; set; }
    public int ProductoId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    public int Unidades { get; set; }
    // Cuando lo generó el bloqueo automático de una recepción
    public int? RecepcionId { get; set; }

    public VentaNav? Venta { get; set; }
    public VentaNavDetalle? VentaDetalle { get; set; }
    public Producto? Producto { get; set; }
    public Recepcion? Recepcion { get; set; }
}
