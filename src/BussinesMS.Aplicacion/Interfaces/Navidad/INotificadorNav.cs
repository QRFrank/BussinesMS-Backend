using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public class NuevaNotificacionNav
{
    public int? TemporadaId { get; set; }
    public EventoNotificacionNav Evento { get; set; }
    /// <summary>1 comprobante a cliente, 2 aviso interno (default), 3 resumen/recordatorio.</summary>
    public int Prioridad { get; set; } = 2;
    public string Mensaje { get; set; } = string.Empty;
    public AudienciaNotificacionNav Audiencia { get; set; }
    public int? AudienciaId { get; set; }
    /// <summary>Solo con Audiencia = TelefonoDirecto.</summary>
    public string? TelefonoDirecto { get; set; }
    public int? ExcluirUsuarioId { get; set; }
    public string? AdjuntoTipo { get; set; }
    public int? ReferenciaId { get; set; }
    /// <summary>Anti-duplicados: si ya existe una notificación vigente con esta clave, no se encola otra.</summary>
    public string? ClaveUnica { get; set; }
    public DateTime? ProgramadaPara { get; set; }
}

/// <summary>
/// Lo único que deben llamar los servicios de negocio para avisar por WhatsApp.
/// Guarda la fila de la bandeja con el repositorio (el SaveChanges ocurre dentro de la transacción
/// INavidadUnitOfWork del servicio llamador, así la operación y su aviso quedan juntos o ninguno).
/// No envía nada: lo hace BandejaWhatsAppJob.
/// </summary>
public interface INotificadorNav
{
    /// <summary>
    /// Devuelve la notificación encolada, o null si no se encoló: WhatsApp no configurado, evento apagado en
    /// ConfigAvisoNav, ClaveUnica repetida, mensaje vacío, audiencia sin id o teléfono directo inválido (se loguea, no lanza).
    /// </summary>
    Task<NotificacionNav?> EncolarAsync(NuevaNotificacionNav nueva);
}
