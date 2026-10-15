using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class ConteoNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? AlmacenId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // 1 = Apertura, 2 = Cierre
    public int? Tipo { get; set; }
}

public class ConteoNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public int CantidadProductos { get; set; }
    public int CantidadDiferencias { get; set; }
    // Vacío en la lista
    public List<ConteoDetalleNavDto> Detalles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConteoDetalleNavDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int StockSistema { get; set; }
    public int Contado { get; set; }
    public int Diferencia { get; set; }
    public string? Observacion { get; set; }
    // 1 Venta no registrada | 2 Consumo | 3 Merma o robo | 4 Error de conteo (null si no hay diferencia o no se indicó)
    public int? MotivoDiferencia { get; set; }
    public string? MotivoDiferenciaNombre { get; set; }
}

public class CrearConteoNavDto
{
    public int AlmacenId { get; set; }
    // 1 = Apertura, 2 = Cierre. La fecha es hoy en Bolivia, salvo el cierre atrasado (ver Fecha).
    public int Tipo { get; set; }
    // Solo para Cierre (tipo 2): día pendiente (apertura sin cierre) anterior a hoy. Con Apertura → 400. Sin valor: hoy.
    public DateTime? Fecha { get; set; }
    public string? Observacion { get; set; }
    public List<CrearConteoDetalleNavDto> Detalles { get; set; } = new();
}

public class CrearConteoDetalleNavDto
{
    public int ProductoId { get; set; }
    public int Contado { get; set; }
    public string? Observacion { get; set; }
    // 1 Venta no registrada | 2 Consumo | 3 Merma o robo | 4 Error de conteo.
    // Obligatorio si la diferencia (contado − stock del sistema) es negativa; opcional si es positiva; se ignora si es 0
    public int? MotivoDiferencia { get; set; }
}

public class ConteoPlantillaNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int StockSistema { get; set; }
}

public class ConteoEstadoNavDto
{
    public int AlmacenId { get; set; }
    public DateTime Fecha { get; set; }
    public bool RequiereConteo { get; set; }
    public bool AperturaHecha { get; set; }
    public bool CierreHecho { get; set; }
    public int? AperturaId { get; set; }
    public int? CierreId { get; set; }
    // Fecha más antigua anterior a `fecha` con apertura sin cierre
    public DateTime? CierrePendienteFecha { get; set; }
}
