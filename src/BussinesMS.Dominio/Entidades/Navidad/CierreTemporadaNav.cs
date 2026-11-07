using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Foto del resultado al cerrar la temporada: no cambia nunca más
public class CierreTemporadaNav : EntidadBase
{
    public int TemporadaId { get; set; }
    // Día (hora Bolivia)
    public DateTime FechaCierre { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    public decimal Ganancia { get; set; }
    public decimal Devengados { get; set; }
    public decimal GananciaNeta { get; set; }
    public decimal DeudasIncobrables { get; set; }
    public int StockSobranteUnidades { get; set; }
    public decimal StockSobranteMonto { get; set; }
    public decimal EfectivoNeto { get; set; }
    public decimal NosDeben { get; set; }
    public decimal Debemos { get; set; }
    public decimal TeQueda { get; set; }
    public decimal CapitalPropio { get; set; }
    public string ResumenJson { get; set; } = string.Empty;

    public Temporada? Temporada { get; set; }
}
