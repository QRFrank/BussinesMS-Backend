namespace BussinesMS.Aplicacion.Comun;

/// <summary>Configuración de WhatsApp (sección "WhatsApp"). Un solo número por temporada.</summary>
public class WhatsAppOpciones
{
    public const string Seccion = "WhatsApp";

    public bool Habilitado { get; set; }
    public EvolutionOpciones Evolution { get; set; } = new();
    public int PausaEntreMensajesSegundos { get; set; } = 4;
    public int MaxMensajesPorHoraPorNumero { get; set; } = 60;
    public int MaxHorasEsperaDesconectado { get; set; } = 24;
    public int IntervaloBandejaSegundos { get; set; } = 30;
    /// <summary>Cada cuánto consulta el monitor el estado de la instancia (default 60 s).</summary>
    public int IntervaloMonitorSegundos { get; set; } = 60;
    // TEMPORAL: quitar antes de producción (MostrarDestinatario)
    public bool MostrarDestinatario { get; set; }

    /// <summary>Configurado = Habilitado y con UrlBase y ApiKey no vacíos.</summary>
    public bool EstaConfigurado =>
        Habilitado
        && !string.IsNullOrWhiteSpace(Evolution.ApiKey)
        && !string.IsNullOrWhiteSpace(Evolution.UrlBase);
}

public class EvolutionOpciones
{
    public string UrlBase { get; set; } = "http://localhost:8080";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 30;
    public int TimeoutEstadoSegundos { get; set; } = 10;
}
