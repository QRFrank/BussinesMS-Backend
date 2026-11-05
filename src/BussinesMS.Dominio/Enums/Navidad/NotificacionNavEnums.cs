namespace BussinesMS.Dominio.Enums.Navidad;

/// <summary>Qué disparó el aviso de WhatsApp. Se guarda como int: agregar valores nuevos al final de cada bloque.</summary>
public enum EventoNotificacionNav
{
    Prueba = 1,

    // Clientes (decisión 9: solo C1 y C7)
    ComprobanteVenta = 10,
    ComprobanteMisVentas = 11,

    // Avisos internos
    CierreCaja = 20,
    AperturaCaja = 21,
    CierreTienda = 22,
    RecepcionAdmin = 23,
    RecepcionAlmacen = 24,
    CargaRuta = 25,
    DevolucionRuta = 26,
    CierreSalidaRuta = 27,
    AbonoVendedor = 28,
    Anulacion = 29,
    ResumenDiario = 30
}

public enum AudienciaNotificacionNav
{
    /// <summary>AudienciaId = ClienteNav.Id; el teléfono se lee de ClienteNav.Telefono al enviar.</summary>
    Cliente = 1,
    /// <summary>AudienciaId = Usuario.Id.</summary>
    Usuario = 2,
    /// <summary>Usuarios activos con rol Admin y acceso al sistema Navideño.</summary>
    Admins = 3,
    /// <summary>AudienciaId = AlmacenId; encargados de ese almacén en la temporada.</summary>
    EncargadosAlmacen = 4,
    /// <summary>AudienciaId = Vendedor.Id; usa el teléfono de su Usuario.</summary>
    Vendedor = 5,
    /// <summary>Número fijo en TelefonoDirecto (solo mensajes de prueba).</summary>
    TelefonoDirecto = 6
}

public enum EstadoEnvioNotificacionNav
{
    Pendiente = 1,
    Enviado = 2,
    Fallido = 3,
    /// <summary>No se envía y no se reintenta (sin teléfono, sin destinatarios, descartado por viejo).</summary>
    Omitido = 4
}
