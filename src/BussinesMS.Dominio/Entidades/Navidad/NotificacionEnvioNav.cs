using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

/// <summary>Envío de una notificación a un destinatario. El teléfono se resuelve justo antes de enviar.</summary>
public class NotificacionEnvioNav : EntidadBase
{
    public int NotificacionId { get; set; }
    /// <summary>Usuario de AuthDB (sin FK).</summary>
    public int? UsuarioId { get; set; }
    public int? ClienteId { get; set; }
    /// <summary>Teléfono +591... usado en el último intento. Null si nunca se resolvió.</summary>
    public string? Destino { get; set; }
    public string DestinatarioDescripcion { get; set; } = string.Empty;
    public EstadoEnvioNotificacionNav Estado { get; set; } = EstadoEnvioNotificacionNav.Pendiente;
    public int Intentos { get; set; }
    public string? UltimoError { get; set; }
    public DateTime? EnviadoEn { get; set; }
    public DateTime? ProximoIntentoEn { get; set; }
    /// <summary>Desde cuándo espera (se reinicia al reintentar a mano). Base del vencimiento.</summary>
    public DateTime PendienteDesde { get; set; } = DateTime.UtcNow;
    public string? InstanciaUsada { get; set; }

    public NotificacionNav? Notificacion { get; set; }
}
