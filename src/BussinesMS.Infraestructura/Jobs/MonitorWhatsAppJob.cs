using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BussinesMS.Infraestructura.Jobs;

/// <summary>Consulta cada cierto tiempo el estado del número y lo deja en WhatsAppInstancia (para el aviso rojo del front).</summary>
public class MonitorWhatsAppJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WhatsAppOpciones> _opciones;
    private readonly ILogger<MonitorWhatsAppJob> _logger;
    private bool _ultimoFueError;

    public MonitorWhatsAppJob(
        IServiceScopeFactory scopeFactory,
        IOptions<WhatsAppOpciones> opciones,
        ILogger<MonitorWhatsAppJob> logger)
    {
        _scopeFactory = scopeFactory;
        _opciones = opciones;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(10, _opciones.Value.IntervaloMonitorSegundos));
        _logger.LogInformation("MonitorWhatsAppJob iniciado (cada {Segundos} s)", intervalo.TotalSeconds);

        using var timer = new PeriodicTimer(intervalo);
        try
        {
            // Primera lectura al arrancar, después cada intervalo
            do
            {
                if (!_opciones.Value.EstaConfigurado)
                    continue;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    scope.ServiceProvider.GetRequiredService<ICurrentUserService>()
                        .SetUser(UsuarioDeSistema.Id, UsuarioDeSistema.Username, 0);
                    var servicio = scope.ServiceProvider.GetRequiredService<IMonitorWhatsAppService>();

                    // Un solo error aislado no prende el aviso rojo: se guarda a partir de la segunda lectura seguida
                    var estado = await servicio.VerificarAsync(persistirError: _ultimoFueError, stoppingToken);
                    _ultimoFueError = estado == EstadoWhatsApp.Error;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en MonitorWhatsAppJob");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // apagado normal
        }
    }
}
