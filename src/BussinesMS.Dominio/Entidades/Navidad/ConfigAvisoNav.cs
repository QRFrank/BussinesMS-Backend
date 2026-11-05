using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

/// <summary>Prende/apaga un evento de aviso. Si no hay fila para el evento, se considera activo.</summary>
public class ConfigAvisoNav : EntidadBase
{
    public EventoNotificacionNav Evento { get; set; }
    public bool Activo { get; set; } = true;
    public TimeOnly? HoraProgramada { get; set; }
    public int? Parametro { get; set; }
}
