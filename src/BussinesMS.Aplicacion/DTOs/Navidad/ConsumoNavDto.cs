using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class ConsumoNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? AlmacenId { get; set; }
    // 1 Propio | 2 Invitación | 3 Otro
    public int? Motivo { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // null → todos; true → solo anulados; false → solo vigentes
    public bool? Anulados { get; set; }
}

public class ConsumoNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public DateTime Fecha { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public int Motivo { get; set; }
    public string MotivoNombre { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public bool Anulado { get; set; }
    public int CantidadProductos { get; set; }
    public int TotalUnidades { get; set; }
    // Solo en GET /{id}: Σ unidades × costo promedio (informativo, pérdida estimada); 0 en la lista
    public decimal CostoEstimado { get; set; }
    // Vacío en la lista
    public List<ConsumoDetalleNavDto> Detalles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConsumoDetalleNavDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
    // Costo promedio del producto (redondeado a 2) y unidades × costo promedio; solo en GET /{id}
    public decimal CostoPromedio { get; set; }
    public decimal CostoEstimado { get; set; }
}

public class CrearConsumoNavDto
{
    // Cualquier almacén navideño activo; si es tienda con conteo exige la apertura de hoy
    public int AlmacenId { get; set; }
    // 1 Propio | 2 Invitación | 3 Otro
    public int Motivo { get; set; }
    public string? Observacion { get; set; }
    public List<CrearConsumoDetalleNavDto> Detalles { get; set; } = new();
}

public class CrearConsumoDetalleNavDto
{
    public int ProductoId { get; set; }
    // Unidades (el front convierte javas + sueltas)
    public int Unidades { get; set; }
}
