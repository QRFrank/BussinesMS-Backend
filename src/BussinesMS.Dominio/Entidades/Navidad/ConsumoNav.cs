using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Consumo propio / invitación / otro: baja stock sin cliente ni cobro. Pérdida = unidades × costo promedio. No se edita: se anula
public class ConsumoNav : EntidadBase
{
    public int TemporadaId { get; set; }
    public DateTime Fecha { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    public MotivoConsumoNav Motivo { get; set; }
    public string? Observacion { get; set; }
    public bool Anulado { get; set; }

    public Temporada? Temporada { get; set; }
    public ICollection<ConsumoNavDetalle> Detalles { get; set; } = new List<ConsumoNavDetalle>();
}
