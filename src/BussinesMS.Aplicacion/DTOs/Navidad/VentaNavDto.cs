using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class VentaNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    // 1 Tienda | 2 Ruta
    public int? Canal { get; set; }
    // 1 Directa | 2 Reserva
    public int? Tipo { get; set; }
    // 1 Contado | 2 Crédito
    public int? CondicionPago { get; set; }
    // 1 Pendiente | 2 Parcial | 3 Pagado
    public int? EstadoPago { get; set; }
    // 1 Pendiente | 2 Parcial | 3 Entregado
    public int? EstadoEntrega { get; set; }
    // true → estadoEntrega distinto de Entregado; false → solo Entregadas (lista de reservas con entrega pendiente)
    public bool? EntregaPendiente { get; set; }
    // true → solo reservas con bloqueo activo
    public bool? Bloqueada { get; set; }
    public int? AlmacenId { get; set; }
    public int? VendedorId { get; set; }
    public int? ClienteId { get; set; }
    public int? NroVenta { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // null → todas; true → solo anuladas; false → solo vigentes
    public bool? Anuladas { get; set; }
}

public class VentaNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int NroVenta { get; set; }
    public DateTime Fecha { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int Canal { get; set; }
    public string CanalNombre { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public int Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public int CondicionPago { get; set; }
    public string CondicionPagoNombre { get; set; } = string.Empty;
    public bool BloquearStock { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal MontoSaldo { get; set; }
    public int EstadoPago { get; set; }
    public string EstadoPagoNombre { get; set; } = string.Empty;
    public int EstadoEntrega { get; set; }
    public string EstadoEntregaNombre { get; set; } = string.Empty;
    public bool Anulada { get; set; }
    public string? Observacion { get; set; }
    public int CantidadProductos { get; set; }
    public int TotalUnidades { get; set; }
    public int UnidadesPendientes { get; set; }
    // Según el usuario logueado: venta de tienda vigente y (admin o vendedor dueño)
    public bool PuedeEditar { get; set; }
    public bool PuedeAnular { get; set; }
    // Solo en crear/editar: productos de una reserva que quedan sobre-reservados (no bloquea)
    public List<string> Advertencias { get; set; } = new();
    // Vacíos en la lista
    public List<VentaNavDetalleDto> Detalles { get; set; } = new();
    public List<EntregaVentaDto> Entregas { get; set; } = new();
    public List<CobroVentaDto> Cobros { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VentaNavDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    // Valores ACTUALES del producto (referencia para el vendedor)
    public decimal PrecioCatalogo { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    public int UnidadesEntregadas { get; set; }
    public int UnidadesPendientes { get; set; }
    public int EstadoEntrega { get; set; }
    public string EstadoEntregaNombre { get; set; } = string.Empty;
}

public class EntregaVentaDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public bool Anulada { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<EntregaVentaDetalleDto> Detalles { get; set; } = new();
}

public class EntregaVentaDetalleDto
{
    public int Id { get; set; }
    public int VentaDetalleId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int Unidades { get; set; }
}

public class CobroVentaDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public int Medio { get; set; }
    public string MedioNombre { get; set; } = string.Empty;
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
    public bool Anulado { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ---------- Crear / editar ----------

public class CrearVentaNavDto
{
    // Tienda navideña activa (EsTienda)
    public int AlmacenId { get; set; }
    // Si el usuario logueado es vendedor activo de la temporada se fuerza ese; si no, obligatorio
    public int? VendedorId { get; set; }
    // Obligatorio si es Reserva, Crédito o si queda saldo
    public int? ClienteId { get; set; }
    // 1 Directa | 2 Reserva
    public int Tipo { get; set; }
    // 1 Contado | 2 Crédito
    public int CondicionPago { get; set; }
    // Solo reservas
    public bool BloquearStock { get; set; }
    public decimal Descuento { get; set; }
    public string? Observacion { get; set; }
    // Contado: obligatorio (monto = neto o sin monto); Crédito: pago inicial opcional (> 0 y < neto)
    public CobroInicialVentaDto? Cobro { get; set; }
    public List<VentaLineaDto> Detalles { get; set; } = new();
}

public class CobroInicialVentaDto
{
    // Contado: null o igual al neto. Crédito: obligatorio
    public decimal? Monto { get; set; }
    // 1 Transferencia | 2 Efectivo
    public MedioPagoNav Medio { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
}

public class VentaLineaDto
{
    public int ProductoId { get; set; }
    // Unidades (el front convierte javas + sueltas)
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
}

// PUT: el conjunto de líneas reemplaza al actual (las que faltan quedan en 0). No se cambia tipo, condición, tienda ni vendedor
public class EditarVentaNavDto
{
    public int? ClienteId { get; set; }
    public decimal Descuento { get; set; }
    public string? Observacion { get; set; }
    public List<VentaLineaDto> Detalles { get; set; } = new();
}

public class RegistrarEntregaVentaDto
{
    // Cualquier almacén navideño activo
    public int AlmacenId { get; set; }
    public List<EntregaLineaDto> Detalles { get; set; } = new();
}

public class EntregaLineaDto
{
    public int VentaDetalleId { get; set; }
    public int Unidades { get; set; }
}

public class RegistrarCobroVentaDto
{
    public decimal Monto { get; set; }
    // 1 Transferencia | 2 Efectivo
    public MedioPagoNav Medio { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
}

public class BloqueoVentaDto
{
    public bool Bloquear { get; set; }
}

// ---------- Auxiliares de la pantalla ----------

public class VentaContextoDto
{
    public bool EsAdmin { get; set; }
    // Vendedor activo de la temporada vinculado al usuario logueado (null si no es vendedor)
    public VendedorActualDto? VendedorActual { get; set; }
}

public class VendedorActualDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
}

public class VentaProductoNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public decimal PrecioCatalogo { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    // En el almacén pedido
    public int Stock { get; set; }
    public int Bloqueado { get; set; }
    // Stock − Bloqueado (mínimo 0)
    public int Disponible { get; set; }
}

// ---------- Cobertura de reservas ----------

public class ReservaCoberturaNavFiltroDto
{
    public int? TemporadaId { get; set; }
}

public class ReservaCoberturaNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    // Σ pendiente de entrega de reservas no anuladas
    public int ReservadoPendiente { get; set; }
    // Parte del reservado pendiente que está bloqueada
    public int Bloqueado { get; set; }
    // Σ stock de todos los almacenes navideños
    public int StockTotal { get; set; }
    // Pedido y todavía no recibido (misma lógica que Faltantes: Σ de las filas con faltante > 0)
    public int PorLlegar { get; set; }
    // ReservadoPendiente > StockTotal + PorLlegar
    public bool SobreReservado { get; set; }
    // (StockTotal + PorLlegar) − ReservadoPendiente: negativo = unidades sobre-reservadas
    public int Diferencia { get; set; }
}
