namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public class ResultadoBandejaNav
{
    public int Resueltas { get; set; }
    public int Enviados { get; set; }
    public int Fallidos { get; set; }
    public int Omitidos { get; set; }
    public int Vencidos { get; set; }
    public bool SinNumeroConectado { get; set; }
}

public interface IBandejaNotificacionNavService
{
    /// <summary>Un ciclo de la bandeja (lo llama BandejaWhatsAppJob). No se ejecuta dos veces a la vez.</summary>
    Task<ResultadoBandejaNav> ProcesarAsync(CancellationToken ct = default);
}
