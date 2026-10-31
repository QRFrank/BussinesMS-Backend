using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Auth;

/// <summary>Instancia de Evolution API (un solo número por temporada). A lo sumo una fila con IsActive = 1.</summary>
public class WhatsAppInstancia : EntidadBase
{
    public string NombreInstancia { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? UltimoEstado { get; set; }
    public DateTime? UltimoEstadoEn { get; set; }
    public string? NumeroEnmascarado { get; set; }
    public string? NombrePerfil { get; set; }
}
