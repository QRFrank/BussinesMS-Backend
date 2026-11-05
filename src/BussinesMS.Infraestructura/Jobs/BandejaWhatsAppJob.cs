using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BussinesMS.Infraestructura.Jobs;

/// <summary>Envía la bandeja de WhatsApp. Un solo bucle: el siguiente ciclo no empieza hasta que termina el anterior.</summary>
public class BandejaWhatsAppJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WhatsAppOpciones> _opciones;
    private readonly ILogger<BandejaWhatsAppJob> _logger;

    public BandejaWhatsAppJob(
        IServiceScopeFactory scopeFactory,
        IOptions<WhatsAppOpciones> opciones,
        ILogger<BandejaWhatsAppJob> logger)
    {
        _scopeFactory = scopeFactory;
        _opciones = opciones;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(5, _opciones.Value.IntervaloBandejaSegundos));
        _logger.LogInformation("BandejaWhatsAppJob iniciado (cada {Segundos} s)", intervalo.TotalSeconds);

        using var timer = new PeriodicTimer(intervalo);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (!_opciones.Value.EstaConfigurado)
                    continue;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    scope.ServiceProvider.GetRequiredService<ICurrentUserService>()
                        .SetUser(UsuarioDeSistema.Id, UsuarioDeSistema.Username, 0);
                    var servicio = scope.ServiceProvider.GetRequiredService<IBandejaNotificacionNavService>();
                    await servicio.ProcesarAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en BandejaWhatsAppJob");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // apagado normal
        }
    }
}
