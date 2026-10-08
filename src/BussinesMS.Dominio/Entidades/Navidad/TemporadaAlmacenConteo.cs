namespace BussinesMS.Dominio.Entidades.Navidad;

// Tabla puente: almacenes (tiendas) que requieren apertura/cierre diario en la temporada.
public class TemporadaAlmacenConteo
{
    public int TemporadaId { get; set; }
    // Almacen vive en AuthDB: sin FK
    public int AlmacenId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUsuarioId { get; set; }

    public Temporada? Temporada { get; set; }
}
