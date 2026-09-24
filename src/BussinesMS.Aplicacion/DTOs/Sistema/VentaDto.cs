using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums;

namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class VentaFiltroDto : GenericPaginationQueryDto
{
    public int? AlmacenId { get; set; }
    public int? SesionCajaId { get; set; }
    public MetodoPago? MetodoPago { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

public class VentaDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int AlmacenId { get; set; }
    public int SesionCajaId { get; set; }
    public DateTime FechaVenta { get; set; }
    public decimal TotalBruto { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal TotalNeto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public string? MotivoDescuento { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public List<VentaDetalleDto> Detalles { get; set; } = [];
}

public class VentaListDto
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int AlmacenId { get; set; }
    public int SesionCajaId { get; set; }
    public DateTime FechaVenta { get; set; }
    public decimal TotalBruto { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal TotalNeto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public string? MotivoDescuento { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public int CantidadDetalles { get; set; }
}

public class CrearVentaDto
{
    public int SesionCajaId { get; set; }
    /// <summary>Opcional. Null o &lt;= 0 → cliente genérico (Id 1).</summary>
    public int? ClienteId { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public decimal? DescuentoTotal { get; set; }
    public string? MotivoDescuento { get; set; }
    public List<CrearVentaDetalleDto> Detalles { get; set; } = [];
}

public class VentaDetalleDto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int VarianteId { get; set; }
    public string? VarianteNombre { get; set; }
    public int LoteId { get; set; }
    public string? CodigoLote { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioUnitarioCobrado { get; set; }
    public decimal CostoUnitarioLote { get; set; }
    public decimal Subtotal { get; set; }
}

public class CrearVentaDetalleDto
{
    public int VarianteId { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioUnitarioCobrado { get; set; }
}
