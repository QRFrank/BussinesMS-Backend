using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class WhatsAppBitacoraFiltroDto : GenericPaginationQueryDto
{
    /// <summary>Fecha local (Bolivia) desde la que se busca, inclusive. Se usa solo la parte de fecha.</summary>
    public DateTime? Desde { get; set; }
    /// <summary>Fecha local (Bolivia) hasta la que se busca, inclusive.</summary>
    public DateTime? Hasta { get; set; }
    /// <summary>Pendiente | Enviado | Fallido | Omitido (nombre o número 1-4).</summary>
    public string? Estado { get; set; }
    /// <summary>Nombre del evento (ej. Prueba, ComprobanteVenta) o su número.</summary>
    public string? Evento { get; set; }
}

public class WhatsAppEnvioDto
{
    public int Id { get; set; }
    public int NotificacionId { get; set; }
    /// <summary>Cuándo se encoló el aviso (UTC, con Z).</summary>
    public DateTime Fecha { get; set; }
    public string Evento { get; set; } = string.Empty;
    public int EventoId { get; set; }
    public int Prioridad { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    /// <summary>+591 7123**** o null si todavía no se resolvió / no tiene teléfono.</summary>
    public string? DestinoEnmascarado { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int Intentos { get; set; }
    public string? UltimoError { get; set; }
    public DateTime? EnviadoEn { get; set; }
    public DateTime? ProximoIntentoEn { get; set; }
    public string? InstanciaUsada { get; set; }
    /// <summary>Primeros ~80 caracteres del mensaje (en el listado).</summary>
    public string MensajeResumen { get; set; } = string.Empty;
    /// <summary>Mensaje completo. Solo en GET envios/{id} (en el listado es null).</summary>
    public string? Mensaje { get; set; }
}

public class WhatsAppEstadoBannerDto
{
    public bool Configurado { get; set; }
    public bool TieneNumero { get; set; }
    /// <summary>Conectado | Desconectado | Conectando | SinConfigurar | Error | SinNumero (último estado guardado por el monitor).</summary>
    public string Estado { get; set; } = "SinNumero";
    /// <summary>true = mostrar el aviso rojo (Desconectado o Error con número vinculado).</summary>
    public bool Caido { get; set; }
    /// <summary>Desde cuándo está en ese estado (UTC, con Z).</summary>
    public DateTime? Desde { get; set; }
    public string? NumeroEnmascarado { get; set; }
    /// <summary>Número vinculado completo (+591XXXXXXXX); null si aún no se conoce.</summary>
    public string? Numero { get; set; }
    /// <summary>Envíos pendientes esperando en la bandeja.</summary>
    public int MensajesEnEspera { get; set; }
}

public class EnviarPruebaBandejaDto
{
    /// <summary>Opcional. Si no viene, se usa el teléfono del usuario que hace la llamada.</summary>
    public string? Telefono { get; set; }
}

public class WhatsAppPruebaBandejaDto
{
    public int NotificacionId { get; set; }
    public string DestinoEnmascarado { get; set; } = string.Empty;
}
