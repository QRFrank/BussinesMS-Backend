using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class Cliente : EntidadBase
{
    /// <summary>Id del "cliente genérico" del sistema (seedeado). No puede modificarse ni eliminarse.</summary>
    public const int ClienteGenericoId = 1;

    public string Nombre { get; set; } = string.Empty;
    public string? NumeroCarnet { get; set; }
    public string? Telefono { get; set; }
}
