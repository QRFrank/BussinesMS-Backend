namespace BussinesMS.Aplicacion.Interfaces.Navidad;

/// <summary>
/// Señal (singleton) para despertar a BandejaWhatsAppJob apenas hay avisos confirmados, sin esperar al intervalo.
/// Las señales se juntan: varias seguidas despiertan una sola vez.
/// </summary>
public interface IBandejaSenal
{
    void Despertar();

    /// <summary>Espera una señal o hasta que pase <paramref name="timeout"/> (respaldo por intervalo).</summary>
    Task EsperarAsync(TimeSpan timeout, CancellationToken ct);
}
