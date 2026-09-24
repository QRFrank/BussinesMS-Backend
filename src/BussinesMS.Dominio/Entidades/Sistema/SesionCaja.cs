using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class SesionCaja : EntidadBase
{
    public int UsuarioId { get; set; }
    public int AlmacenId { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierre { get; set; }
    public decimal MontoInicial { get; set; } = 0;
    public decimal IngresosEfectivo { get; set; } = 0;
    public decimal IngresosDigitales { get; set; } = 0;
    public decimal EgresosGastos { get; set; } = 0;
    public decimal EgresosPagoProveedor { get; set; } = 0;
    public decimal? MontoEsperadoEfectivo { get; set; }
    public decimal? MontoRealEntregado { get; set; }
    public decimal? Diferencia { get; set; }
    public EstadoSesionCaja Estado { get; set; } = EstadoSesionCaja.Abierta;
}
