using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class MovimientoNav : EntidadBase
{
    public int TemporadaId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    public int ProductoId { get; set; }
    // Positivo = ingreso, negativo = salida
    public int Cantidad { get; set; }
    public TipoMovimientoNav Tipo { get; set; }
    // Recepcion, Compra… (Ajuste 2: sin lotes)
    public string ReferenciaTipo { get; set; } = string.Empty;
    public int ReferenciaId { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    // UTC
    public DateTime Fecha { get; set; }
    public string? Motivo { get; set; }

    public Temporada? Temporada { get; set; }
    public Producto? Producto { get; set; }
}
