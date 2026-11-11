using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

// Registro personal y privado de ventas del vendedor de ruta a sus clientes

// ---------- Lista ----------

public class MisVentasRutaFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? ClienteId { get; set; }
    // Por Fecha de la venta (solo la fecha)
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    // Solo ventas no anuladas con saldo > 0
    public bool SoloConSaldo { get; set; }
    public bool IncluirAnuladas { get; set; }
}

public class VentaClienteRutaListaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int? SalidaRutaId { get; set; }
    // true si la venta está vinculada a una salida que sigue Abierta
    public bool SalidaAbierta { get; set; }
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
    public bool Anulada { get; set; }
    public int CantidadLineas { get; set; }
}

// ---------- Detalle ----------

public class VentaClienteRutaDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public DateTime Fecha { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int? SalidaRutaId { get; set; }
    // true si la venta está vinculada a una salida que sigue Abierta (el vínculo solo se puede cambiar en ese caso)
    public bool SalidaAbierta { get; set; }
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
    public bool Anulada { get; set; }
    public string? Observacion { get; set; }
    public List<VentaClienteRutaLineaDetalleDto> Detalles { get; set; } = new();
    public List<VentaClienteRutaCobroDto> Cobros { get; set; } = new();
    // Obsoleto: siempre vacío (el exceso sobre lo que tiene en poder ahora se rechaza con 400)
    public List<string> Avisos { get; set; } = new();
    // Solo al registrar con enviarComprobante=true: true = quedó en la bandeja; false = no se pudo encolar (WhatsApp sin configurar, aviso apagado o error). Null = no se pidió
    public bool? ComprobanteEncolado { get; set; }
}

public class VentaClienteRutaLineaDetalleDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class VentaClienteRutaCobroDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    // 1 Transferencia | 2 Efectivo
    public int? Medio { get; set; }
    public string? MedioNombre { get; set; }
    public bool Anulado { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---------- Requests ----------

public class RegistrarVentaClienteRutaDto
{
    public int ClienteId { get; set; }
    // Omitida = su salida Abierta (si no hay, queda sin salida). Si se indica, debe estar Abierta
    public int? SalidaRutaId { get; set; }
    // true = venta "sin ruta": queda sin salida aunque tenga una abierta (ignora salidaRutaId). Por defecto false
    public bool SinSalida { get; set; }
    // Omitida = hoy (Bolivia)
    public DateTime? Fecha { get; set; }
    public string? Observacion { get; set; }
    // Cobrado al registrar (0 = todo a crédito)
    public decimal PagadoAhora { get; set; }
    // Obligatorio si PagadoAhora > 0
    public MedioPagoNav? Medio { get; set; }
    // Opcional: pago por medio (1 o 2 líneas, un medio una sola vez). Si viene con elementos reemplaza a pagadoAhora/medio (el comprobante de cada línea no se guarda en cobros de ruta)
    public List<PagoMedioDto>? Pagos { get; set; }
    // Opcional (false por defecto). Si es true, telefonoComprobante es obligatorio y se encola el comprobante PDF por WhatsApp a ese número
    public bool EnviarComprobante { get; set; }
    // Celular al que se envía el comprobante (solo para este envío; no se guarda en el cliente)
    public string? TelefonoComprobante { get; set; }
    public List<VentaClienteRutaLineaDto> Detalles { get; set; } = new();
}

public class VentaClienteRutaLineaDto
{
    public int ProductoId { get; set; }
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class EditarVentaClienteRutaDto
{
    public int ClienteId { get; set; }
    // Omitida = se conserva la fecha actual
    public DateTime? Fecha { get; set; }
    public string? Observacion { get; set; }
    // null = no cambia el vínculo con la salida. true = pasar a "sin ruta" (permitido si la salida actual está abierta o si no tenía salida). false = pasar a "en ruta" (se vincula a la salida abierta actual del vendedor; 400 si no tiene). Si ya está en ese estado no hace nada. Con la salida vinculada cerrada o anulada y un cambio real: 409 SALIDA_CERRADA
    public bool? SinSalida { get; set; }
    // null = no se tocan los cobros; con valor corrige el pago inicial (0 o 1 cobro vigente)
    public decimal? PagadoAhora { get; set; }
    // Obligatorio si PagadoAhora > 0
    public MedioPagoNav? Medio { get; set; }
    // Opcional: pago por medio. Si viene con elementos reemplaza los cobros vigentes (máx. uno por medio) y a pagadoAhora/medio
    public List<PagoMedioDto>? Pagos { get; set; }
    public List<VentaClienteRutaLineaDto> Detalles { get; set; } = new();
}

public class RegistrarCobroClienteRutaDto
{
    public decimal Monto { get; set; }
    // Omitida = hoy (Bolivia)
    public DateTime? Fecha { get; set; }
    public MedioPagoNav Medio { get; set; }
    // Opcional: pago por medio (1 o 2 líneas). Si viene con elementos reemplaza a monto/medio
    public List<PagoMedioDto>? Pagos { get; set; }
}

// ---------- Me deben ----------

public class MisVentasMeDebenDto
{
    public decimal TotalGeneral { get; set; }
    public List<MisVentasMeDebenClienteDto> Clientes { get; set; } = new();
}

public class MisVentasMeDebenClienteDto
{
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal SaldoTotal { get; set; }
    public int VentasPendientes { get; set; }
    public List<MisVentasMeDebenVentaDto> Ventas { get; set; } = new();
}

public class MisVentasMeDebenVentaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
}

// ---------- Ayuda del cierre ----------

public class MisVentasSalidaResumenDto
{
    public int SalidaRutaId { get; set; }
    public DateTime FechaApertura { get; set; }
    // 1 Abierta | 2 Cerrada | 3 Anulada
    public int Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    // Σ total de sus ventas vigentes de la salida (sin la venta excluida)
    public decimal TotalRegistrado { get; set; }
    // Productos cargados en la salida
    public List<MisVentasSalidaProductoDto> Productos { get; set; } = new();
}

public class MisVentasSalidaProductoDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public decimal PrecioCatalogo { get; set; }
    public int Cargado { get; set; }
    public int Devuelto { get; set; }
    // cargado - devuelto
    public int EnPoder { get; set; }
    // Unidades en sus ventas vigentes de la salida (sin la venta excluida)
    public int Registrado { get; set; }
    // max(0, enPoder - registrado)
    public int Disponible { get; set; }
}

public class MisVentasTotalSalidaDto
{
    public int SalidaRutaId { get; set; }
    public decimal TotalRegistrado { get; set; }
    public int CantidadVentas { get; set; }
}
