using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

/// <summary>Bitácora, reintento, estado para el banner y prueba por la bandeja (todo solo Admin).</summary>
public class WhatsAppBandejaAdminService : IWhatsAppBandejaAdminService
{
    private readonly INotificacionEnvioNavRepository _envios;
    private readonly INotificadorNav _notificador;
    private readonly IWhatsAppInstanciaRepository _instancias;
    private readonly IUsuarioRepository _usuarios;
    private readonly ICurrentUserService _currentUser;
    private readonly IOptions<WhatsAppOpciones> _opciones;
    private readonly ILogger<WhatsAppBandejaAdminService> _logger;

    public WhatsAppBandejaAdminService(
        INotificacionEnvioNavRepository envios,
        INotificadorNav notificador,
        IWhatsAppInstanciaRepository instancias,
        IUsuarioRepository usuarios,
        ICurrentUserService currentUser,
        IOptions<WhatsAppOpciones> opciones,
        ILogger<WhatsAppBandejaAdminService> logger)
    {
        _envios = envios;
        _notificador = notificador;
        _instancias = instancias;
        _usuarios = usuarios;
        _currentUser = currentUser;
        _opciones = opciones;
        _logger = logger;
    }

    // ---------------- Bitácora ----------------

    public async Task<PagedResultDto<WhatsAppEnvioDto>> ObtenerBitacoraAsync(WhatsAppBitacoraFiltroDto filtro)
    {
        var consulta = _envios.AsQueryable().AsNoTracking().Include(e => e.Notificacion).Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(filtro.Estado))
        {
            if (!TryParsearEnum<EstadoEnvioNotificacionNav>(filtro.Estado, out var estado))
                throw new ExcepcionDominio("El estado no es válido (Pendiente, Enviado, Fallido u Omitido)", 400, "FILTRO_INVALIDO");
            consulta = consulta.Where(e => e.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Evento))
        {
            if (!TryParsearEnum<EventoNotificacionNav>(filtro.Evento, out var evento))
                throw new ExcepcionDominio("El evento no es válido", 400, "FILTRO_INVALIDO");
            consulta = consulta.Where(e => e.Notificacion!.Evento == evento);
        }

        // Desde/Hasta son fechas locales de Bolivia; se comparan contra la fecha de creación del aviso (UTC)
        if (filtro.Desde.HasValue)
        {
            var (inicio, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(filtro.Desde.Value));
            consulta = consulta.Where(e => e.Notificacion!.CreatedAt >= inicio);
        }
        if (filtro.Hasta.HasValue)
        {
            var (_, fin) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(filtro.Hasta.Value));
            consulta = consulta.Where(e => e.Notificacion!.CreatedAt < fin);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Filter))
        {
            var f = filtro.Filter.Trim().ToLower();
            consulta = consulta.Where(e => e.DestinatarioDescripcion.ToLower().Contains(f)
                                           || e.Notificacion!.Mensaje.ToLower().Contains(f));
        }

        var total = await consulta.CountAsync();
        var pagina = filtro.GetPageValue();
        var tamano = filtro.GetPageSizeValue();

        var items = await consulta
            .OrderByDescending(e => e.Notificacion!.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return new PagedResultDto<WhatsAppEnvioDto>
        {
            Items = items.Select(e => Mapear(e, incluirMensaje: false)).ToList(),
            TotalCount = total,
            Page = pagina,
            PageSize = tamano
        };
    }

    public async Task<WhatsAppEnvioDto> ObtenerEnvioAsync(int id)
    {
        var envio = await _envios.ObtenerConNotificacionAsync(id);
        if (envio == null || !envio.IsActive)
            throw new ExcepcionDominio("Envío no encontrado", 404, "ENTIDAD_NO_ENCONTRADA");
        return Mapear(envio, incluirMensaje: true);
    }

    // ---------------- Reintentar ----------------

    public async Task<WhatsAppEnvioDto> ReintentarAsync(int id)
    {
        var envio = await _envios.ObtenerConNotificacionAsync(id);
        if (envio == null || !envio.IsActive)
            throw new ExcepcionDominio("Envío no encontrado", 404, "ENTIDAD_NO_ENCONTRADA");

        if (envio.Estado is not (EstadoEnvioNotificacionNav.Fallido or EstadoEnvioNotificacionNav.Omitido))
            throw new ExcepcionDominio("Solo se pueden reintentar los envíos fallidos u omitidos", 409, "ENVIO_NO_REINTENTABLE");

        envio.Estado = EstadoEnvioNotificacionNav.Pendiente;
        envio.Intentos = 0;
        envio.UltimoError = null;
        envio.ProximoIntentoEn = null;
        envio.PendienteDesde = DateTime.UtcNow; // reinicia el vencimiento
        await _envios.ActualizarAsync(envio);

        _logger.LogInformation("Envío de WhatsApp {Id} vuelto a Pendiente por el usuario {Usuario}", id, _currentUser.GetUsuarioId());
        return Mapear(envio, incluirMensaje: true);
    }

    // ---------------- Estado para el banner ----------------

    public async Task<WhatsAppEstadoBannerDto> ObtenerEstadoBannerAsync()
    {
        var fila = await _instancias.ObtenerVigenteAsync();
        var configurado = _opciones.Value.EstaConfigurado;
        var enEspera = await _envios.ContarPendientesAsync();

        if (!configurado)
        {
            return new WhatsAppEstadoBannerDto
            {
                Configurado = false,
                TieneNumero = fila != null,
                Estado = "SinConfigurar",
                Caido = false,
                MensajesEnEspera = enEspera
            };
        }

        if (fila == null)
        {
            return new WhatsAppEstadoBannerDto
            {
                Configurado = true,
                TieneNumero = false,
                Estado = "SinNumero",
                Caido = false,
                MensajesEnEspera = enEspera
            };
        }

        var estado = string.IsNullOrWhiteSpace(fila.UltimoEstado) ? "Desconectado" : fila.UltimoEstado;
        return new WhatsAppEstadoBannerDto
        {
            Configurado = true,
            TieneNumero = true,
            Estado = estado,
            Caido = estado is "Desconectado" or "Error",
            Desde = EnUtc(fila.UltimoEstadoEn),
            NumeroEnmascarado = fila.NumeroEnmascarado,
            MensajesEnEspera = enEspera
        };
    }

    // ---------------- Prueba por la bandeja ----------------

    public async Task<WhatsAppPruebaBandejaDto> EnviarPruebaPorBandejaAsync(EnviarPruebaBandejaDto dto)
    {
        if (!_opciones.Value.EstaConfigurado)
            throw new ExcepcionDominio("Falta configurar Evolution en el servidor", 409, "WHATSAPP_NO_CONFIGURADO");

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        var mensaje = "Prueba de la bandeja de BussinesMS (" + BoliviaTimeZone.Now().ToString("dd/MM/yyyy HH:mm")
                      + "). Si lo recibiste, el circuito completo de avisos funciona.";

        var nueva = new NuevaNotificacionNav
        {
            Evento = EventoNotificacionNav.Prueba,
            Prioridad = 2,
            Mensaje = mensaje
        };
        string destino;

        if (!string.IsNullOrWhiteSpace(dto?.Telefono))
        {
            if (!NormalizadorTelefono.TryNormalizar(dto.Telefono, out var normalizado))
                throw new ExcepcionDominio("El teléfono no es válido", 400, "TELEFONO_INVALIDO");
            nueva.Audiencia = AudienciaNotificacionNav.TelefonoDirecto;
            nueva.TelefonoDirecto = normalizado;
            destino = normalizado;
        }
        else
        {
            var usuario = (await _usuarios.ObtenerPorIdsAsync(new[] { usuarioId })).FirstOrDefault();
            if (usuario == null || !NormalizadorTelefono.TryNormalizar(usuario.Telefono, out var propio))
                throw new ExcepcionDominio("Tu usuario no tiene teléfono: cargalo en Usuarios o indicá un teléfono", 400, "USUARIO_SIN_TELEFONO");
            nueva.Audiencia = AudienciaNotificacionNav.Usuario;
            nueva.AudienciaId = usuarioId;
            destino = propio;
        }

        var notificacion = await _notificador.EncolarAsync(nueva)
            ?? throw new ExcepcionDominio("No se pudo encolar el mensaje de prueba", 409, "NO_ENCOLADO");

        var enmascarado = EnmascaradorTelefono.Enmascarar(destino);
        _logger.LogInformation("Prueba de WhatsApp encolada (notificación {Id}) para {Destino}", notificacion.Id, enmascarado);
        return new WhatsAppPruebaBandejaDto { NotificacionId = notificacion.Id, DestinoEnmascarado = enmascarado };
    }

    // ---------------- helpers ----------------

    private static bool TryParsearEnum<T>(string valor, out T resultado) where T : struct, Enum
    {
        resultado = default;
        if (!Enum.TryParse<T>(valor.Trim(), ignoreCase: true, out var r) || !Enum.IsDefined(r))
            return false;
        resultado = r;
        return true;
    }

    private static DateTime? EnUtc(DateTime? valor)
        => valor.HasValue ? DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc) : null;

    private static WhatsAppEnvioDto Mapear(NotificacionEnvioNav e, bool incluirMensaje)
    {
        var n = e.Notificacion!;
        var resumen = n.Mensaje.Length <= 80 ? n.Mensaje : n.Mensaje[..80] + "…";
        return new WhatsAppEnvioDto
        {
            Id = e.Id,
            NotificacionId = e.NotificacionId,
            Fecha = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc),
            Evento = n.Evento.ToString(),
            EventoId = (int)n.Evento,
            Prioridad = n.Prioridad,
            Destinatario = e.DestinatarioDescripcion,
            DestinoEnmascarado = string.IsNullOrEmpty(e.Destino) ? null : EnmascaradorTelefono.Enmascarar(e.Destino),
            Estado = e.Estado.ToString(),
            Intentos = e.Intentos,
            UltimoError = e.UltimoError,
            EnviadoEn = EnUtc(e.EnviadoEn),
            ProximoIntentoEn = EnUtc(e.ProximoIntentoEn),
            InstanciaUsada = e.InstanciaUsada,
            MensajeResumen = resumen.Replace("\n", " "),
            Mensaje = incluirMensaje ? n.Mensaje : null
        };
    }
}
