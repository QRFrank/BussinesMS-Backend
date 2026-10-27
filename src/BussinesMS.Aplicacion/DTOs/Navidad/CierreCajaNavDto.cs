using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class CierreCajaNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? AlmacenId { get; set; }
    // Día (hora Bolivia) del conteo de cierre del turno
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

// Cierre de caja registrado (entrega de efectivo de un turno)
public class CierreCajaNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int ConteoAperturaId { get; set; }
    public int ConteoCierreId { get; set; }
    // Día del conteo de cierre
    public DateTime Fecha { get; set; }
    // Horas del turno, ya en hora de Bolivia
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal EfectivoEsperado { get; set; }
    public decimal Transferencias { get; set; }
    public decimal Credito { get; set; }
    public decimal EfectivoEntregado { get; set; }
    // entregado − esperado: + sobra, − falta
    public decimal Diferencia { get; set; }
    public string? Observacion { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// GET resumen: turno cerrado sin entrega (Pendiente = true) o turno actual para previsualizar (Pendiente = false)
public class CierreCajaResumenNavDto
{
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    // true: el turno ya está cerrado y falta registrar la entrega (se puede hacer POST). false: turno abierto, solo previsualización
    public bool Pendiente { get; set; }
    public int ConteoAperturaId { get; set; }
    // null en la previsualización
    public int? ConteoCierreId { get; set; }
    // Horas del turno en hora de Bolivia (Hasta = ahora en la previsualización)
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal EfectivoEsperado { get; set; }
    public decimal Transferencias { get; set; }
    // Desglose informativo del efectivo por origen (suman efectivoEsperado)
    public decimal EfectivoVentasTurno { get; set; }
    public decimal EfectivoReservas { get; set; }
    public decimal EfectivoCobrosDeuda { get; set; }
    // Desglose informativo de las transferencias por origen (suman transferencias)
    public decimal TransferenciasVentasTurno { get; set; }
    public decimal TransferenciasReservas { get; set; }
    public decimal TransferenciasCobrosDeuda { get; set; }
    public decimal Credito { get; set; }
    public List<CierreCajaVentaNavDto> Ventas { get; set; } = new();
    public List<CierreCajaCobroNavDto> Cobros { get; set; } = new();
}

public class CierreCajaVentaNavDto
{
    public int Id { get; set; }
    public int NroVenta { get; set; }
    public DateTime Hora { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public decimal MontoNeto { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal MontoSaldo { get; set; }
}

public class CierreCajaCobroNavDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int NroVenta { get; set; }
    public DateTime Hora { get; set; }
    public decimal Monto { get; set; }
    // 1 Transferencia | 2 Efectivo
    public int Medio { get; set; }
    public string MedioNombre { get; set; } = string.Empty;
    // "VentaTurno" (venta directa creada en el turno) | "Reserva" (venta tipo Reserva) | "CobroDeuda" (venta directa de antes del turno)
    public string Origen { get; set; } = string.Empty;
    public string OrigenNombre { get; set; } = string.Empty;
    public string TipoVenta { get; set; } = string.Empty;
    public string? ClienteNombre { get; set; }
}

public class RegistrarCierreCajaNavDto
{
    public int AlmacenId { get; set; }
    public decimal EfectivoEntregado { get; set; }
    // Obligatoria si EfectivoEntregado ≠ EfectivoEsperado. Máx. 500
    public string? Observacion { get; set; }
}
