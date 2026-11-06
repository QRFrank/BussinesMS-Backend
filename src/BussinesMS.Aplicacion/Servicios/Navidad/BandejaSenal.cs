using BussinesMS.Aplicacion.Interfaces.Navidad;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

public class BandejaSenal : IBandejaSenal
{
    private readonly SemaphoreSlim _semaforo = new(0, 1);

    public void Despertar()
    {
        try
        {
            if (_semaforo.CurrentCount == 0) _semaforo.Release();
        }
        catch (SemaphoreFullException)
        {
            // ya hay una señal pendiente
        }
    }

    public async Task EsperarAsync(TimeSpan timeout, CancellationToken ct)
        => await _semaforo.WaitAsync(timeout, ct);
}
