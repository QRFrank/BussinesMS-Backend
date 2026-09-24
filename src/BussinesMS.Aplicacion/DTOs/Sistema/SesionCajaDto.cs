using BussinesMS.Dominio.Enums;

namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class SesionCajaDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int AlmacenId { get; set; }
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal MontoInicial { get; set; }
    public decimal IngresosEfectivo { get; set; }
    public decimal IngresosDigitales { get; set; }
    public decimal EgresosGastos { get; set; }
    public decimal EgresosPagoProveedor { get; set; }
    public decimal? MontoEsperadoEfectivo { get; set; }
    public decimal? MontoRealEntregado { get; set; }
    public decimal? Diferencia { get; set; }
    public EstadoSesionCaja Estado { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearSesionCajaDto
{
    public int AlmacenId { get; set; }
    public decimal MontoInicial { get; set; }
}

public class CerrarSesionCajaDto
{
    public decimal MontoRealEntregado { get; set; }
}
