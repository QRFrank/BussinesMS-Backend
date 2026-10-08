using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class GastoNav : EntidadBase
{
    public int TemporadaId { get; set; }
    public int CategoriaId { get; set; }
    public DateTime Fecha { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }

    public Temporada? Temporada { get; set; }
    public CategoriaGastoNav? Categoria { get; set; }
}
