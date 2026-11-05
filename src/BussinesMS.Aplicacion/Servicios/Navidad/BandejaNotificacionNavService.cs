using System.Diagnostics;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

/// <summary>
/// Procesa la bandeja de WhatsApp: genera los envíos por destinatario, vence los viejos y manda los pendientes
/// por prioridad con pausa, tope por hora y reintentos. Solo usa repositorios.
/// </summary>
public class BandejaNotificacionNavService : IBandejaNotificacionNavService
{
    private const int MaxIntentos = 3;
    private const int MaxNotificacionesPorCiclo = 50;
    private const int MaxEnviosPorCiclo = 100;
    private const int HorasMaxResumen = 12;
    private static readonly TimeSpan PresupuestoCiclo = TimeSpan.FromSeconds(50);

    // Un solo bucle a la vez dentro del proceso (el job ya es secuencial; esto protege contra usos manuales)
    private static readonly SemaphoreSlim Candado = new(1, 1);

    private readonly INotificacionNavRepository _notificaciones;
    private readonly INotificacionEnvioNavRepository _envios;
    private readonly IUsuarioRepository _usuarios;
    private readonly IVendedorNavRepository _vendedores;
    private readonly IClienteNavRepository _clientes;
    private readonly ITemporadaAlmacenEncargadoRepository _encargados;
    private readonly ITemporadaRepository _temporadas;
    private readonly IWhatsAppInstanciaRepository _instancias;
    private readonly IEvolutionApiClient _evolution;
    private readonly INavidadUnitOfWork _uow;
    private readonly IOptions<WhatsAppOpciones> _opciones;
    private readonly ILogger<BandejaNotificacionNavService> _logger;

    public BandejaNotificacionNavService(
        INotificacionNavRepository notificaciones,
        INotificacionEnvioNavRepository envios,
        IUsuarioRepository usuarios,
        IVendedorNavRepository vendedores,
        IClienteNavRepository clientes,
        ITemporadaAlmacenEncargadoRepository encargados,
        ITemporadaRepository temporadas,
        IWhatsAppInstanciaRepository instancias,
        IEvolutionApiClient evolution,
        INavidadUnitOfWork uow,
        IOptions<WhatsAppOpciones> opciones,
        ILogger<BandejaNotificacionNavService> logger)
    {
        _notificaciones = notificaciones;
        _envios = envios;
        _usuarios = usuarios;
        _vendedores = vendedores;
        _clientes = clientes;
        _encargados = encargados;
        _temporadas = temporadas;
        _instancias = instancias;
        _evolution = evolution;
        _uow = uow;
        _opciones = opciones;
        _logger = logger;
    }

    private sealed record Destinatario(int? UsuarioId, int? ClienteId, string Descripcion, string? Omitir);

    public async Task<ResultadoBandejaNav> ProcesarAsync(CancellationToken ct = default)
    {
        var resultado = new ResultadoBandejaNav();
        if (!_opciones.Value.EstaConfigurado)
            return resultado;

        if (!await Candado.WaitAsync(0, ct))
            return resultado;

        try
        {
            await ResolverNotificacionesAsync(resultado, ct);
            await VencerAsync(resultado);
            await EnviarPendientesAsync(resultado, ct);
        }
        finally
        {
            Candado.Release();
        }

        if (resultado.Resueltas + resultado.Enviados + resultado.Fallidos + resultado.Omitidos + resultado.Vencidos > 0)
            _logger.LogInformation(
                "Bandeja WhatsApp: {Resueltas} resueltas, {Enviados} enviados, {Fallidos} fallidos, {Omitidos} omitidos, {Vencidos} vencidos",
                resultado.Resueltas, resultado.Enviados, resultado.Fallidos, resultado.Omitidos, resultado.Vencidos);

        return resultado;
    }

    // ---------------- 1. Generar envíos por destinatario ----------------

    private async Task ResolverNotificacionesAsync(ResultadoBandejaNav resultado, CancellationToken ct)
    {
        var ahora = DateTime.UtcNow;
        var pendientes = await _notificaciones.ObtenerSinResolverAsync(ahora, MaxNotificacionesPorCiclo);

        foreach (var n in pendientes)
        {
            ct.ThrowIfCancellationRequested();

            var destinatarios = await ResolverDestinatariosAsync(n);
            if (destinatarios.Count == 0)
                destinatarios.Add(new Destinatario(null, null, "Sin destinatarios", "No hay destinatarios para este aviso"));

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var d in destinatarios)
                {
                    await _envios.CrearAsync(new NotificacionEnvioNav
                    {
                        NotificacionId = n.Id,
                        UsuarioId = d.UsuarioId,
                        ClienteId = d.ClienteId,
                        DestinatarioDescripcion = Truncar(d.Descripcion, 200),
                        Estado = d.Omitir == null ? EstadoEnvioNotificacionNav.Pendiente : EstadoEnvioNotificacionNav.Omitido,
                        UltimoError = d.Omitir,
                        PendienteDesde = ahora
                    });
                }

                n.ResueltaEn = ahora;
                await _notificaciones.ActualizarAsync(n);
                await _uow.CommitAsync();
                resultado.Resueltas++;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
    }

    private async Task<List<Destinatario>> ResolverDestinatariosAsync(NotificacionNav n)
    {
        var lista = new List<Destinatario>();

        switch (n.Audiencia)
        {
            case AudienciaNotificacionNav.Cliente:
            {
                var cliente = n.AudienciaId.HasValue ? await _clientes.ObtenerPorIdAsync(n.AudienciaId.Value) : null;
                if (cliente == null || !cliente.IsActive)
                    lista.Add(new Destinatario(null, n.AudienciaId, "Cliente #" + n.AudienciaId, "El cliente ya no existe"));
                else
                    lista.Add(new Destinatario(null, cliente.Id, "Cliente " + cliente.Nombre, null));
                break;
            }
            case AudienciaNotificacionNav.Usuario:
                lista.AddRange(await DestinatariosUsuariosAsync(
                    n.AudienciaId.HasValue ? new[] { n.AudienciaId.Value } : Array.Empty<int>(), n.ExcluirUsuarioId, ""));
                break;
            case AudienciaNotificacionNav.Admins:
            {
                var admins = await _usuarios.ObtenerAdminsNavidadAsync();
                foreach (var u in admins.Where(u => u.Id != n.ExcluirUsuarioId))
                    lista.Add(new Destinatario(u.Id, null, "Admin " + NombreCompleto(u), null));
                break;
            }
            case AudienciaNotificacionNav.EncargadosAlmacen:
            {
                var temporadaId = n.TemporadaId ?? (await _temporadas.ObtenerAbiertaAsync())?.Id;
                if (temporadaId.HasValue && n.AudienciaId.HasValue)
                {
                    var asignaciones = await _encargados.ObtenerPorTemporadaAsync(temporadaId.Value);
                    var ids = asignaciones
                        .Where(a => a.IsActive && a.AlmacenId == n.AudienciaId.Value)
                        .Select(a => a.UsuarioId)
                        .Distinct()
                        .ToList();
                    lista.AddRange(await DestinatariosUsuariosAsync(ids, n.ExcluirUsuarioId, "Encargado "));
                }
                break;
            }
            case AudienciaNotificacionNav.Vendedor:
            {
                var vendedor = n.AudienciaId.HasValue ? await _vendedores.ObtenerPorIdAsync(n.AudienciaId.Value) : null;
                if (vendedor != null && vendedor.IsActive)
                    lista.AddRange(await DestinatariosUsuariosAsync(new[] { vendedor.UsuarioId }, n.ExcluirUsuarioId, "Vendedor "));
                break;
            }
            case AudienciaNotificacionNav.TelefonoDirecto:
                lista.Add(new Destinatario(null, null, "Teléfono indicado", null));
                break;
        }

        return lista;
    }

    private async Task<List<Destinatario>> DestinatariosUsuariosAsync(IEnumerable<int> ids, int? excluir, string prefijo)
    {
        var usuarios = await _usuarios.ObtenerPorIdsAsync(ids.Where(i => i != excluir));
        return usuarios.Select(u => new Destinatario(u.Id, null, prefijo + NombreCompleto(u), null)).ToList();
    }

    private static string NombreCompleto(Usuario u) => (u.Nombre + " " + u.Apellido).Trim();

    // ---------------- 2. Vencimiento ----------------

    private async Task VencerAsync(ResultadoBandejaNav resultado)
    {
        var ahora = DateTime.UtcNow;
        var horasMax = Math.Max(1, _opciones.Value.MaxHorasEsperaDesconectado);
        var horasMin = Math.Min(HorasMaxResumen, horasMax);

        var candidatos = await _envios.ObtenerPendientesAntiguosAsync(ahora.AddHours(-horasMin), 200);
        foreach (var e in candidatos)
        {
            var esResumen = e.Notificacion?.Prioridad >= 3;
            var horas = esResumen ? horasMin : horasMax;
            if (ahora - e.PendienteDesde <= TimeSpan.FromHours(horas)) continue;

            if (esResumen)
            {
                e.Estado = EstadoEnvioNotificacionNav.Omitido;
                e.UltimoError = "Descartado: venció sin poder enviarse (" + horas + " h)";
            }
            else
            {
                e.Estado = EstadoEnvioNotificacionNav.Fallido;
                e.UltimoError = "Venció: no se pudo enviar en " + horas + " h (WhatsApp sin conexión)";
            }
            e.ProximoIntentoEn = null;
            await _envios.ActualizarAsync(e);
            resultado.Vencidos++;
        }
    }

    // ---------------- 3. Envío ----------------

    private async Task EnviarPendientesAsync(ResultadoBandejaNav resultado, CancellationToken ct)
    {
        if (await _envios.ContarPendientesAsync() == 0)
            return;

        var fila = await _instancias.ObtenerVigenteAsync();
        if (fila == null)
        {
            resultado.SinNumeroConectado = true;
            return;
        }

        var estado = await _evolution.ObtenerEstadoAsync(fila.NombreInstancia, ct);
        if (estado.Estado != EstadoWhatsApp.Conectado)
        {
            resultado.SinNumeroConectado = true;
            _logger.LogDebug("Bandeja WhatsApp en espera: número no conectado ({Estado})", estado.Estado);
            return;
        }

        var ahora = DateTime.UtcNow;
        var tope = Math.Max(1, _opciones.Value.MaxMensajesPorHoraPorNumero);
        var cupo = tope - await _envios.ContarEnviadosDesdeAsync(ahora.AddHours(-1));
        if (cupo <= 0)
        {
            _logger.LogInformation("Bandeja WhatsApp: tope de {Tope} mensajes por hora alcanzado", tope);
            return;
        }

        var pendientes = await _envios.ObtenerPendientesParaEnviarAsync(ahora, MaxEnviosPorCiclo);
        if (pendientes.Count == 0)
            return;

        var usuarios = (await _usuarios.ObtenerPorIdsAsync(
                pendientes.Where(p => p.UsuarioId.HasValue).Select(p => p.UsuarioId!.Value)))
            .ToDictionary(u => u.Id);
        var pausa = Math.Max(0, _opciones.Value.PausaEntreMensajesSegundos);
        var reloj = Stopwatch.StartNew();

        for (var i = 0; i < pendientes.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (reloj.Elapsed > PresupuestoCiclo) break;

            var envio = pendientes[i];
            if (envio.Estado != EstadoEnvioNotificacionNav.Pendiente) continue;

            var (telefono, motivo) = await ResolverTelefonoAsync(envio, usuarios);
            if (telefono == null)
            {
                envio.Estado = EstadoEnvioNotificacionNav.Omitido;
                envio.UltimoError = motivo;
                envio.ProximoIntentoEn = null;
                await _envios.ActualizarAsync(envio);
                resultado.Omitidos++;
                continue;
            }

            if (cupo <= 0) break;

            try
            {
                await _evolution.EnviarTextoAsync(fila.NombreInstancia, telefono, envio.Notificacion!.Mensaje, ct);

                envio.Intentos++;
                envio.Estado = EstadoEnvioNotificacionNav.Enviado;
                envio.EnviadoEn = DateTime.UtcNow;
                envio.Destino = telefono;
                envio.InstanciaUsada = fila.NombreInstancia;
                envio.UltimoError = null;
                envio.ProximoIntentoEn = null;
                await _envios.ActualizarAsync(envio); // se guarda después de cada envío: si la API cae no se repite
                cupo--;
                resultado.Enviados++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ExcepcionDominio ex) when (ex.CodigoError is "EVOLUTION_NO_DISPONIBLE" or "EVOLUTION_APIKEY_INVALIDA" or "WHATSAPP_NO_CONFIGURADO")
            {
                // Problema de conexión/configuración: no gasta intentos, el resto espera al próximo ciclo
                _logger.LogWarning("Bandeja WhatsApp detenida: {Codigo}", ex.CodigoError);
                resultado.SinNumeroConectado = true;
                break;
            }
            catch (Exception ex)
            {
                var mensaje = ex is ExcepcionDominio ed ? ed.Message : "Error al enviar (" + ex.GetType().Name + ")";
                await RegistrarFalloAsync(envio, telefono, fila.NombreInstancia, mensaje);
                if (envio.Estado == EstadoEnvioNotificacionNav.Fallido) resultado.Fallidos++;
            }

            if (i < pendientes.Count - 1 && pausa > 0)
                await Task.Delay(TimeSpan.FromSeconds(pausa) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)), ct);
        }
    }

    private async Task RegistrarFalloAsync(NotificacionEnvioNav envio, string telefono, string instancia, string mensaje)
    {
        envio.Intentos++;
        envio.Destino = telefono;
        envio.InstanciaUsada = instancia;
        envio.UltimoError = Truncar(mensaje, 500);
        if (envio.Intentos >= MaxIntentos)
        {
            envio.Estado = EstadoEnvioNotificacionNav.Fallido;
            envio.ProximoIntentoEn = null;
        }
        else
        {
            envio.ProximoIntentoEn = DateTime.UtcNow.AddMinutes(envio.Intentos == 1 ? 1 : 5);
        }
        await _envios.ActualizarAsync(envio);
        _logger.LogWarning("Falló un envío de WhatsApp a {Destino} (intento {Intento}): {Error}",
            EnmascaradorTelefono.Enmascarar(telefono), envio.Intentos, envio.UltimoError);
    }

    /// <summary>Resuelve el teléfono justo antes de enviar. Devuelve (null, motivo) si no se puede enviar.</summary>
    private async Task<(string? Telefono, string? Motivo)> ResolverTelefonoAsync(NotificacionEnvioNav envio, Dictionary<int, Usuario> usuarios)
    {
        if (envio.ClienteId.HasValue)
        {
            var cliente = await _clientes.ObtenerPorIdAsync(envio.ClienteId.Value);
            if (cliente == null || !cliente.IsActive) return (null, "El cliente ya no existe");
            if (string.IsNullOrWhiteSpace(cliente.Telefono)) return (null, "El cliente no tiene teléfono");
            return NormalizadorTelefono.TryNormalizar(cliente.Telefono, out var t)
                ? (t, null)
                : (null, "El teléfono del cliente no es válido");
        }

        if (envio.UsuarioId.HasValue)
        {
            if (!usuarios.TryGetValue(envio.UsuarioId.Value, out var u) || !u.IsActive)
                return (null, "El usuario está inactivo o ya no existe");
            if (string.IsNullOrWhiteSpace(u.Telefono)) return (null, "El usuario no tiene teléfono");
            return NormalizadorTelefono.TryNormalizar(u.Telefono, out var t)
                ? (t, null)
                : (null, "El teléfono del usuario no es válido");
        }

        var directo = envio.Notificacion?.TelefonoDirecto;
        if (string.IsNullOrWhiteSpace(directo)) return (null, "Sin teléfono");
        return NormalizadorTelefono.TryNormalizar(directo, out var d) ? (d, null) : (null, "El teléfono no es válido");
    }

    private static string Truncar(string valor, int max) => valor.Length <= max ? valor : valor[..max];
}
