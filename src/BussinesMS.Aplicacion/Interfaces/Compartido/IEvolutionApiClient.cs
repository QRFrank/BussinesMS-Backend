namespace BussinesMS.Aplicacion.Interfaces.Compartido;

public enum EstadoWhatsApp
{
    Conectado,
    Desconectado,
    Conectando,
    NoExiste,
    Error,
    SinConfigurar
}

public class EstadoInstanciaEvolution
{
    public EstadoWhatsApp Estado { get; set; }
    public string? NumeroJid { get; set; }
    public string? NombrePerfil { get; set; }
    public string? Detalle { get; set; }
}

public class QrInstanciaEvolution
{
    /// <summary>Imagen lista para img src (data:image/png;base64,...). Null si aún no está lista o ya está conectado.</summary>
    public string? QrBase64 { get; set; }
    public EstadoWhatsApp Estado { get; set; }
}

public interface IEvolutionApiClient
{
    /// <summary>No lanza por errores de red: devuelve Estado=Error con Detalle.</summary>
    Task<EstadoInstanciaEvolution> ObtenerEstadoAsync(string instancia, CancellationToken ct = default);
    Task CrearInstanciaAsync(string instancia, CancellationToken ct = default);
    Task<QrInstanciaEvolution> ObtenerQrAsync(string instancia, CancellationToken ct = default);
    Task DesconectarAsync(string instancia, CancellationToken ct = default);
    Task BorrarInstanciaAsync(string instancia, CancellationToken ct = default);
    /// <param name="numeroNormalizado">Formato +591XXXXXXXX (se envía sin '+').</param>
    Task EnviarTextoAsync(string instancia, string numeroNormalizado, string texto, CancellationToken ct = default);
}
