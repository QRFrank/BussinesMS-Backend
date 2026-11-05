using BussinesMS.Aplicacion.Interfaces.Compartido;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IMonitorWhatsAppService
{
    /// <summary>
    /// Consulta el estado de la instancia vigente y actualiza WhatsAppInstancia (estado y ultimoEstadoEn cuando cambia).
    /// El estado Error (Evolution inalcanzable) solo se guarda si <paramref name="persistirError"/> es true
    /// (el job lo pide a partir de la segunda lectura seguida con error). Devuelve el estado leído, o null si no aplica.
    /// </summary>
    Task<EstadoWhatsApp?> VerificarAsync(bool persistirError, CancellationToken ct = default);
}
