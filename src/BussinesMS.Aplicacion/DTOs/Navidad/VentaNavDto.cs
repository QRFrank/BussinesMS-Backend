using BussinesMS.Aplicacion.Seguridad;

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
    // Solo ventas con al menos un detalle vigente (Unidades > 0) de este producto
    public int? ProductoId { get; set; }
}

public class VentaNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int NroVenta { get; set; }
    public DateTime Fecha { get; set; }
    // Null en las reservas (no pertenecen a una tienda)
    public int? AlmacenId { get; set; }
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
    // Obsoleto: true si la reserva tiene algún bloqueo vigente (equivale a unidadesBloqueadas > 0)
    public bool BloquearStock { get; set; }
    // Σ bloqueos vigentes de la reserva (0 en ventas directas)
    public int UnidadesBloqueadas { get; set; }
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
    // Solo en la lista filtrada por productoId: unidades de ese producto en la venta; null si no se filtró
    public int? UnidadesProducto { get; set; }
    // Solo en la lista filtrada por productoId: unidades entregadas de ese producto en la venta; null si no se filtró
    public int? UnidadesEntregadasProducto { get; set; }
    // Según el usuario logueado: venta de tienda vigente y (admin o vendedor dueño)
    public bool PuedeEditar { get; set; }
    public bool PuedeAnular { get; set; }
    // Obsoleto: ya no se llena (las reservas sin cobertura se rechazan con 409 SIN_COBERTURA_PEDIDO)
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
    [OcultoSinPrecioCompra] public decimal? PrecioCompraUnidad { get; set; }
    public int UnidadesEntregadas { get; set; }
    public int UnidadesPendientes { get; set; }
    public int EstadoEntrega { get; set; }
    public string EstadoEntregaNombre { get; set; } = string.Empty;
    // Solo reservas: stock bloqueado de la línea, parte sin bloqueo (pendiente - bloqueado) y detalle por almacén
    public int Bloqueado { get; set; }
    public int SinBloqueo { get; set; }
    public List<ReservaBloqueoNavDto> Bloqueos { get; set; } = new();
}

public class ReservaBloqueoNavDto
{
    public int Id { get; set; }
    public int VentaDetalleId { get; set; }
    public int ProductoId { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int Unidades { get; set; }
    // Si lo generó la recepción de un pedido
    public int? RecepcionId { get; set; }
    public DateTime CreatedAt { get; set; }
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
    // 1 Directa (la 2 Reserva se rechaza: las reservas se registran en api/Navidad/Reservas)
    public int Tipo { get; set; }
    // 1 Contado | 2 Crédito
    public int CondicionPago { get; set; }
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
    // Opcional: tienda de la sesión donde se recibe el dinero (caja de tienda). Debe ser una tienda navideña activa
    public int? AlmacenId { get; set; }
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
    [OcultoSinPrecioCompra] public decimal? PrecioCompraUnidad { get; set; }
    // En el almacén pedido
    public int Stock { get; set; }
    public int Bloqueado { get; set; }
    // Stock − Bloqueado (mínimo 0)
    public int Disponible { get; set; }
}
