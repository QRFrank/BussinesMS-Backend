using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

public class NotificadorNav : INotificadorNav
{
    private const int MaxLargoMensaje = 4000;

    private readonly INotificacionNavRepository _notificaciones;
    private readonly IConfigAvisoNavRepository _config;
    private readonly INavidadUnitOfWork _uow;
    private readonly IOptions<WhatsAppOpciones> _opciones;
    private readonly ILogger<NotificadorNav> _logger;

    public NotificadorNav(
        INotificacionNavRepository notificaciones,
        IConfigAvisoNavRepository config,
        INavidadUnitOfWork uow,
        IOptions<WhatsAppOpciones> opciones,
        ILogger<NotificadorNav> logger)
    {
        _notificaciones = notificaciones;
        _config = config;
        _uow = uow;
        _opciones = opciones;
        _logger = logger;
    }

    public async Task<NotificacionNav?> EncolarAsync(NuevaNotificacionNav nueva)
    {
        if (!_opciones.Value.EstaConfigurado)
            return null;

        if (string.IsNullOrWhiteSpace(nueva.Mensaje))
        {
            _logger.LogWarning("Aviso de WhatsApp {Evento} sin mensaje: no se encola", nueva.Evento);
            return null;
        }

        string? telefonoDirecto = null;
        if (nueva.Audiencia == AudienciaNotificacionNav.TelefonoDirecto)
        {
            if (!NormalizadorTelefono.TryNormalizar(nueva.TelefonoDirecto, out var normalizado))
            {
                _logger.LogWarning("Aviso de WhatsApp {Evento} con teléfono directo inválido: no se encola", nueva.Evento);
                return null;
            }
            telefonoDirecto = normalizado;
        }
        else if (nueva.Audiencia != AudienciaNotificacionNav.Admins && !nueva.AudienciaId.HasValue)
        {
            _logger.LogWarning("Aviso de WhatsApp {Evento} para {Audiencia} sin AudienciaId: no se encola", nueva.Evento, nueva.Audiencia);
            return null;
        }

        // Prueba no se puede apagar; el resto sí desde ConfigAvisoNav (sin fila = activo)
        if (nueva.Evento != EventoNotificacionNav.Prueba)
        {
            var cfg = await _config.ObtenerPorEventoAsync(nueva.Evento);
            if (cfg != null && !cfg.Activo)
                return null;
        }

        var clave = string.IsNullOrWhiteSpace(nueva.ClaveUnica) ? null : nueva.ClaveUnica.Trim();
        if (clave != null && await _notificaciones.ExisteClaveUnicaAsync(clave))
        {
            _logger.LogInformation("Aviso de WhatsApp duplicado ({Clave}): no se encola otra vez", clave);
            return null;
        }

        var mensaje = nueva.Mensaje.Trim();
        if (mensaje.Length > MaxLargoMensaje)
            mensaje = mensaje[..MaxLargoMensaje];

        var entidad = new NotificacionNav
        {
            TemporadaId = nueva.TemporadaId,
            Evento = nueva.Evento,
            Prioridad = Math.Clamp(nueva.Prioridad, 1, 3),
            Mensaje = mensaje,
            Audiencia = nueva.Audiencia,
            AudienciaId = nueva.AudienciaId,
            TelefonoDirecto = telefonoDirecto,
            ExcluirUsuarioId = nueva.ExcluirUsuarioId,
            AdjuntoTipo = nueva.AdjuntoTipo,
            ReferenciaId = nueva.ReferenciaId,
            ClaveUnica = clave,
            ProgramadaPara = nueva.ProgramadaPara
        };

        var creada = await _notificaciones.CrearAsync(entidad);
        // Despierta a la bandeja (despues del Commit si hay transaccion abierta)
        _uow.MarcarAvisosPendientes();
        return creada;
    }
}
