using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

public class MonitorWhatsAppService : IMonitorWhatsAppService
{
    private readonly IWhatsAppInstanciaRepository _repositorio;
    private readonly IEvolutionApiClient _evolution;
    private readonly IOptions<WhatsAppOpciones> _opciones;
    private readonly ILogger<MonitorWhatsAppService> _logger;

    public MonitorWhatsAppService(
        IWhatsAppInstanciaRepository repositorio,
        IEvolutionApiClient evolution,
        IOptions<WhatsAppOpciones> opciones,
        ILogger<MonitorWhatsAppService> logger)
    {
        _repositorio = repositorio;
        _evolution = evolution;
        _opciones = opciones;
        _logger = logger;
    }

    public async Task<EstadoWhatsApp?> VerificarAsync(bool persistirError, CancellationToken ct = default)
    {
        if (!_opciones.Value.EstaConfigurado)
            return null;

        var fila = await _repositorio.ObtenerVigenteAsync();
        if (fila == null)
            return null;

        var estado = await _evolution.ObtenerEstadoAsync(fila.NombreInstancia, ct);
        if (estado.Estado == EstadoWhatsApp.SinConfigurar)
            return null;

        var nombre = estado.Estado switch
        {
            EstadoWhatsApp.Conectado => "Conectado",
            EstadoWhatsApp.Conectando => "Conectando",
            EstadoWhatsApp.Error => "Error",
            _ => "Desconectado" // Desconectado y NoExiste
        };

        if (estado.Estado == EstadoWhatsApp.Error && !persistirError)
            return estado.Estado;

        var cambio = false;
        var estadoAnterior = fila.UltimoEstado;

        if (fila.UltimoEstado != nombre)
        {
            fila.UltimoEstado = nombre;
            cambio = true;
        }

        if (estado.Estado == EstadoWhatsApp.Conectado)
        {
            var enmascarado = EnmascaradorTelefono.Enmascarar(estado.NumeroJid);
            if (!string.IsNullOrEmpty(enmascarado) && fila.NumeroEnmascarado != enmascarado)
            {
                fila.NumeroEnmascarado = enmascarado;
                cambio = true;
            }
            if (!string.IsNullOrWhiteSpace(estado.NombrePerfil) && fila.NombrePerfil != estado.NombrePerfil)
            {
                fila.NombrePerfil = estado.NombrePerfil.Length <= 100 ? estado.NombrePerfil : estado.NombrePerfil[..100];
                cambio = true;
            }
        }

        // Sin cambio: no se toca ultimoEstadoEn (es "desde cuándo está así")
        if (!cambio)
            return estado.Estado;

        if (estadoAnterior != nombre || fila.UltimoEstadoEn == null)
            fila.UltimoEstadoEn = DateTime.UtcNow;
        await _repositorio.ActualizarAsync(fila);

        if (estadoAnterior != nombre)
            _logger.LogInformation("WhatsApp: estado {Anterior} -> {Nuevo} (instancia {Instancia})", estadoAnterior, nombre, fila.NombreInstancia);

        return estado.Estado;
    }
}
