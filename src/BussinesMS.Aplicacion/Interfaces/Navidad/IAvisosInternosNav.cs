using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

/// <summary>Datos de una anulación para el aviso S1 a los admins.</summary>
public sealed record AnulacionAviso(
    int TemporadaId,
    /// <summary>Qué se anuló: "Venta", "Cobro de venta", "Recepción", etc.</summary>
    string Tipo,
    /// <summary>Identificador legible: "N° 152", "Salida N° 12", etc.</summary>
    string Referencia,
    /// <summary>Anti-duplicados: clave estable de lo anulado (ej. "venta:15"). Se le antepone "anulacion:".</summary>
    string Clave,
    string? Detalle = null,
    /// <summary>Si la anulación es de un vendedor de ruta, se agrega "Vendedor: Nombre" al texto.</summary>
    int? VendedorId = null);

/// <summary>
/// Avisos internos inmediatos de WhatsApp (fase 3). Los llaman los servicios de negocio DENTRO de su transacción
/// (INavidadUnitOfWork), justo antes del Commit. Arman el texto y encolan con INotificadorNav; no guardan nada aparte
/// ni envían. Nunca lanzan: si algo falla se registra en el log y la operación sigue.
/// El autor de la operación (usuario actual) no recibe los avisos a admins y encargados; sí los de comprobante al vendedor.
/// </summary>
public interface IAvisosInternosNav
{
    /// <summary>Apertura o cierre de tienda (conteo): admins + el vendedor que lo hizo.</summary>
    Task TiendaAsync(ConteoInventario conteo);

    /// <summary>Cierre de caja (siempre; destaca la diferencia): admins + el vendedor que lo hizo.</summary>
    Task CierreCajaAsync(CierreCajaTienda caja);

    /// <summary>A3 a admins y E1 a los encargados de cada almacén de destino.</summary>
    Task RecepcionAsync(Recepcion recepcion, string proveedorNombre);

    /// <summary>V1 al vendedor, A4 a admins y E2 a los encargados de cada almacén de origen.</summary>
    Task CargaRutaAsync(SalidaRuta salida, SalidaRutaCarga carga);

    /// <summary>V1 al vendedor, A4 a admins y E2 a los encargados de cada almacén de destino.</summary>
    Task DevolucionRutaAsync(SalidaRuta salida, DevolucionRuta devolucion);

    /// <summary>A5 a admins y V2 al vendedor. <paramref name="venta"/> es null si devolvió todo (sin venta).</summary>
    Task CierreSalidaAsync(SalidaRuta salida, VentaNav? venta, decimal dineroEntregado, MedioPagoNav? medio);

    /// <summary>V2 al vendedor: abono registrado y saldo restante.</summary>
    Task AbonoVendedorAsync(AbonoVendedor abono);

    /// <summary>Traslado registrado: aviso inmediato a admins (sin el autor).</summary>
    Task TrasladoAsync(Traslado traslado);

    /// <summary>S1 a admins.</summary>
    Task AnulacionAsync(AnulacionAviso anulacion);
}
