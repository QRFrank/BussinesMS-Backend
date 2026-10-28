using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

// ---------- Contexto ----------

public class PagoVendedorContextoDto
{
    public bool EsAdmin { get; set; }
    // Vendedor de la temporada vinculado al usuario (de cualquier tipo), null si no lo es
    public int? VendedorId { get; set; }
    public bool PuedeVerTodo { get; set; }
    // Registrar y anular pagos: solo admin
    public bool PuedeRegistrar { get; set; }
    // Admin: todos los vendedores activos; vendedor: solo su fila
    public List<PagoVendedorVendedorItemDto> Vendedores { get; set; } = new();
}

public class PagoVendedorVendedorItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public decimal? SueldoMensual { get; set; }
}

// ---------- Reporte de ventas por vendedor ----------

public class ReporteVentasVendedorFiltroDto
{
    public int? TemporadaId { get; set; }
    public int? VendedorId { get; set; }
    // 1 Tienda | 2 Ruta | null todos
    public int? Canal { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
}

public class ReporteVentasVendedorDto
{
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public int Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public List<ReporteVentasVendedorProductoDto> Productos { get; set; } = new();
    public ReporteVentasVendedorTotalesDto Totales { get; set; } = new();
    // Solo informativos y solo para el admin (null para el vendedor)
    public decimal? MontoCobrado { get; set; }
    public decimal? Saldo { get; set; }
    // Pago de comisión vigente en la temporada (null si no hay)
    public int? PagoComisionVigenteId { get; set; }
}

public class ReporteVentasVendedorProductoDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int UnidadesRuta { get; set; }
    public int UnidadesTienda { get; set; }
    public int Unidades { get; set; }
    // Σ subtotal de las líneas, sin descuento
    public decimal MontoBruto { get; set; }
    // Con la parte proporcional del descuento de cada venta
    public decimal MontoNeto { get; set; }
}

public class ReporteVentasVendedorTotalesDto
{
    public int UnidadesRuta { get; set; }
    public int UnidadesTienda { get; set; }
    public int Unidades { get; set; }
    public decimal MontoBruto { get; set; }
    public decimal Descuentos { get; set; }
    public decimal MontoNeto { get; set; }
    public int CantidadVentas { get; set; }
}

// ---------- Pagos ----------

public class PagoVendedorFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? VendedorId { get; set; }
    // 1 Sueldo | 2 Comisión
    public int? Tipo { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    // Por defecto (false) solo vigentes
    public bool IncluirAnulados { get; set; }
}

public class PagoVendedorDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public string? Periodo { get; set; }
    public int? ModoComision { get; set; }
    public string? ModoComisionNombre { get; set; }
    public decimal? Porcentaje { get; set; }
    public decimal? MontoBase { get; set; }
    public decimal Monto { get; set; }
    public int Medio { get; set; }
    public string MedioNombre { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public bool Anulado { get; set; }
    // Solo en el detalle y en comisión por producto
    public List<PagoVendedorDetalleDto> Detalles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class PagoVendedorDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
    public decimal ComisionUnidad { get; set; }
    public decimal Subtotal { get; set; }
}

public class RegistrarPagoVendedorDto
{
    public int VendedorId { get; set; }
    public TipoPagoVendedor Tipo { get; set; }
    // Por defecto, hoy (Bolivia)
    public DateTime? Fecha { get; set; }
    public MedioPagoNav Medio { get; set; }
    public string? Observacion { get; set; }

    // Sueldo
    // "yyyy-MM"
    public string? Periodo { get; set; }
    public decimal? Monto { get; set; }

    // Comisión
    public ModoComisionVendedor? Modo { get; set; }
    public decimal? Porcentaje { get; set; }
    // Modo por producto: comisión por unidad escrita por producto; los productos que no vengan cuentan 0
    public List<PagoVendedorLineaDto>? Lineas { get; set; }
}

public class PagoVendedorLineaDto
{
    public int ProductoId { get; set; }
    public decimal ComisionUnidad { get; set; }
}

// ---------- Sueldos ----------

public class SueldoVendedorResumenDto
{
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    // Sugerido para el pago
    public decimal? SueldoMensual { get; set; }
    // Meses desde el mes de inicio de la temporada hasta el mes actual (o el de cierre), ambos inclusive
    public int Meses { get; set; }
    public decimal TotalPagado { get; set; }
    // sueldoMensual × meses − totalPagado (null si el vendedor no tiene sueldo mensual)
    public decimal? Debe { get; set; }
    public List<SueldoVendedorPeriodoDto> Periodos { get; set; } = new();
}

public class SueldoVendedorPeriodoDto
{
    public string Periodo { get; set; } = string.Empty;
    public decimal Pagado { get; set; }
}
