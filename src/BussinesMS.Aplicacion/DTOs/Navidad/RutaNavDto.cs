using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

// ---------- Contexto ----------

public class RutaContextoDto
{
    public bool EsAdmin { get; set; }
    public bool EsEncargado { get; set; }
    // Vendedor de ruta activo de la temporada vinculado al usuario logueado (null si no lo es)
    public int? VendedorId { get; set; }
    // Solo el admin ve todas las salidas, deudas y abonos; los demás ven solo lo suyo
    public bool PuedeVerTodo { get; set; }
    // Solo el admin registra abonos
    public bool PuedeAbonar { get; set; }
    // Admin (cualquier salida) o vendedor de ruta (la suya)
    public bool PuedeCerrar { get; set; }
    public RutaVendedorItemDto? VendedorActual { get; set; }
    public List<RutaVendedorItemDto> VendedoresRuta { get; set; } = new();
    // Solo si es encargado: ids de los almacenes que tiene a cargo; si no, null
    public List<int>? AlmacenesACargo { get; set; }
}

public class RutaVendedorItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

// ---------- Salidas: lista y detalle ----------

public class SalidaRutaFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? VendedorId { get; set; }
    // 1 Abierta | 2 Cerrada | 3 Anulada
    public int? Estado { get; set; }
    // Por FechaApertura (solo la fecha)
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}

public class SalidaRutaListaDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public int Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    // cargado − devuelto (vigentes) mientras la salida está Abierta; 0 si ya no lo está
    public int UnidadesEnPoder { get; set; }
    // cargado − devuelto (vigentes)
    public int UnidadesVendidas { get; set; }
    // MontoNeto de la venta creada al cerrar (null si no hay venta)
    public decimal? MontoVenta { get; set; }
    public int? VentaId { get; set; }
    public int? NroVenta { get; set; }
}

public class SalidaRutaDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public int Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public string? Observacion { get; set; }
    public int? VentaId { get; set; }
    public ResumenVentaRutaDto? Venta { get; set; }
    public int UnidadesCargadas { get; set; }
    public int UnidadesDevueltas { get; set; }
    public int UnidadesEnPoder { get; set; }
    public int UnidadesVendidas { get; set; }
    // Σ vendido × precio de catálogo actual
    public decimal TotalCatalogo { get; set; }
    public List<SalidaRutaProductoDto> Productos { get; set; } = new();
    public List<SalidaRutaCargaDto> Cargas { get; set; } = new();
    public List<DevolucionRutaDto> Devoluciones { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ResumenVentaRutaDto
{
    public int Id { get; set; }
    public int NroVenta { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal MontoSaldo { get; set; }
    public int EstadoPago { get; set; }
    public string EstadoPagoNombre { get; set; } = string.Empty;
    public bool Anulada { get; set; }
}

public class SalidaRutaProductoDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    // Cargas y devoluciones vigentes (no anuladas; el devuelto incluye las tardías)
    public int Cargado { get; set; }
    public int Devuelto { get; set; }
    // cargado − devuelto si la salida está Abierta; 0 en otro caso
    public int EnPoder { get; set; }
    // cargado − devuelto (en una salida abierta es lo que se vendería al cerrar)
    public int Vendido { get; set; }
    public decimal PrecioCatalogo { get; set; }
    // vendido × precioCatalogo
    public decimal Subtotal { get; set; }
    // Solo con venta vigente: precio y unidades reales de su línea
    public decimal? PrecioUnitario { get; set; }
    public int? UnidadesVenta { get; set; }
}

public class SalidaRutaCargaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public bool Anulada { get; set; }
    public List<SalidaRutaCargaLineaDto> Detalles { get; set; } = new();
}

public class SalidaRutaCargaLineaDto
{
    public int Id { get; set; }
    public int AlmacenOrigenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
}

public class DevolucionRutaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public bool EsTardia { get; set; }
    public bool Anulada { get; set; }
    public string? Observacion { get; set; }
    public List<DevolucionRutaLineaDto> Detalles { get; set; } = new();
}

public class DevolucionRutaLineaDto
{
    public int Id { get; set; }
    public int AlmacenDestinoId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
}

// ---------- Cargas, devoluciones y cierre ----------

public class CargarSalidaRutaDto
{
    // Admin/encargado: obligatorio (vendedor de ruta activo de la temporada). Vendedor de ruta logueado: se ignora (queda él)
    public int? VendedorId { get; set; }
    public string? Observacion { get; set; }
    public List<CargaLineaRutaDto> Detalles { get; set; } = new();
}

public class CargaLineaRutaDto
{
    public int AlmacenOrigenId { get; set; }
    public int ProductoId { get; set; }
    // Unidades (el front convierte javas + sueltas)
    public int Unidades { get; set; }
}

public class DevolverSalidaRutaDto
{
    public string? Observacion { get; set; }
    public List<DevolucionLineaRutaDto> Detalles { get; set; } = new();
}

public class DevolucionLineaRutaDto
{
    public int AlmacenDestinoId { get; set; }
    public int ProductoId { get; set; }
    public int Unidades { get; set; }
}

public class CerrarSalidaRutaDto
{
    // Dinero que entrega el vendedor al cerrar (0 o null = nada). Se aplica a la venta de esta salida
    public decimal? DineroEntregado { get; set; }
    // 1 Transferencia | 2 Efectivo. Obligatorio si dineroEntregado > 0
    public MedioPagoNav? Medio { get; set; }
    public string? Comprobante { get; set; }
    // Descuento global de la venta (rebaja), >= 0
    public decimal Descuento { get; set; }
    public string? Observacion { get; set; }
}

// ---------- Abonos y deuda ----------

public class AbonoVendedorFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? VendedorId { get; set; }
    // Por defecto (false) solo vigentes
    public bool IncluirAnulados { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}

public class AbonoVendedorDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal Descuento { get; set; }
    public int? Medio { get; set; }
    public string? MedioNombre { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
    public bool Anulado { get; set; }
    // Vacío en la lista
    public List<AbonoVendedorAplicacionDto> Aplicaciones { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class AbonoVendedorAplicacionDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int NroVenta { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal MontoDescuento { get; set; }
}

public class CrearAbonoVendedorDto
{
    public int VendedorId { get; set; }
    // Dinero (>= 0)
    public decimal Monto { get; set; }
    // Descuento (>= 0). monto + descuento > 0
    public decimal Descuento { get; set; }
    // 1 Transferencia | 2 Efectivo. Obligatorio si monto > 0
    public MedioPagoNav? Medio { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
}

public class DeudaVendedorRutaDto
{
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public int SalidasAbiertas { get; set; }
    // Σ (cargado − devuelto) de sus salidas Abiertas
    public int UnidadesEnPoder { get; set; }
    // Σ unidades de las líneas de sus ventas de ruta no anuladas
    public int UnidadesVendidas { get; set; }
    public decimal MontoVendido { get; set; }
    public decimal Descuentos { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal Saldo { get; set; }
}

// ---------- Editar venta de ruta (solo admin) ----------

public class EditarPreciosVentaRutaDto
{
    // null = deja el actual
    public decimal? Descuento { get; set; }
    public List<PrecioLineaRutaDto> Detalles { get; set; } = new();
}

public class PrecioLineaRutaDto
{
    public int ProductoId { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class DevolverVentaRutaDto
{
    public int AlmacenDestinoId { get; set; }
    public string? Observacion { get; set; }
    public List<DevolverVentaLineaRutaDto> Detalles { get; set; } = new();
}

public class DevolverVentaLineaRutaDto
{
    public int ProductoId { get; set; }
    public int Unidades { get; set; }
}
