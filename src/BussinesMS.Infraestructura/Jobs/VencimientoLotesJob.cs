using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Infraestructura.Jobs;

public class VencimientoLotesJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VencimientoLotesJob> _logger;

    public VencimientoLotesJob(
        IServiceScopeFactory scopeFactory,
        ILogger<VencimientoLotesJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VencimientoLotesJob iniciado");

        try
        {
            await ProcesarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en VencimientoLotesJob (disparo inicial)");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var (inicioHoyUtc, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(DateTime.UtcNow));
            var proximaMedianocheUtc = inicioHoyUtc.AddHours(24);
            var espera = proximaMedianocheUtc - DateTime.UtcNow;

            if (espera <= TimeSpan.Zero)
                espera = TimeSpan.FromHours(24);

            try
            {
                await Task.Delay(espera, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await ProcesarAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en VencimientoLotesJob");
            }
        }
    }

    private async Task ProcesarAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var vencimientoService = scope.ServiceProvider
            .GetRequiredService<IVencimientoLoteService>();

        var procesados = await vencimientoService.ProcesarVencimientosPendientesAsync();
        _logger.LogInformation("VencimientoLotesJob: {Cantidad} lotes procesados exitosamente", procesados);
    }
}
