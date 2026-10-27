using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Entrega de efectivo al cerrar un turno de tienda (apertura → cierre). Los montos quedan tal como estaban al registrar
public class CierreCajaTienda : EntidadBase
{
    public int TemporadaId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    public int ConteoAperturaId { get; set; }
    public int ConteoCierreId { get; set; }
    // Horas del turno (UTC): apertura y cierre
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal EfectivoEsperado { get; set; }
    public decimal Transferencias { get; set; }
    public decimal Credito { get; set; }
    public decimal EfectivoEntregado { get; set; }
    // EfectivoEntregado - EfectivoEsperado
    public decimal Diferencia { get; set; }
    public string? Observacion { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }

    public Temporada? Temporada { get; set; }
    public ConteoInventario? ConteoApertura { get; set; }
    public ConteoInventario? ConteoCierre { get; set; }
}
