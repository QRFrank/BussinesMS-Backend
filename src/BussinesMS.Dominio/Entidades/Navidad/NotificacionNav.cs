using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

/// <summary>
/// Aviso de WhatsApp en la bandeja de salida. Se guarda en la misma transacción que la operación que lo origina.
/// Los destinatarios (NotificacionEnvioNav) se resuelven al procesar la bandeja, no al encolar.
/// </summary>
public class NotificacionNav : EntidadBase
{
    public int? TemporadaId { get; set; }
    public EventoNotificacionNav Evento { get; set; }
    /// <summary>1 comprobante a cliente, 2 aviso interno, 3 resumen/recordatorio.</summary>
    public int Prioridad { get; set; } = 2;
    public string Mensaje { get; set; } = string.Empty;
    public AudienciaNotificacionNav Audiencia { get; set; }
    /// <summary>ClienteId / UsuarioId / AlmacenId / VendedorId según la audiencia (sin FK).</summary>
    public int? AudienciaId { get; set; }
    /// <summary>Solo para Audiencia = TelefonoDirecto (normalizado +591...).</summary>
    public string? TelefonoDirecto { get; set; }
    public int? ExcluirUsuarioId { get; set; }
    public string? AdjuntoTipo { get; set; }
    public int? ReferenciaId { get; set; }
    /// <summary>Anti-duplicados (índice único filtrado), ej. "comprobante-venta:152".</summary>
    public string? ClaveUnica { get; set; }
    public DateTime? ProgramadaPara { get; set; }
    /// <summary>Cuándo se generaron los envíos por destinatario. Null = todavía sin resolver.</summary>
    public DateTime? ResueltaEn { get; set; }

    public ICollection<NotificacionEnvioNav> Envios { get; set; } = new List<NotificacionEnvioNav>();
}
