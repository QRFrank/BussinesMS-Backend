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
    public decimal MontoEfectivo { get; set; }
    public decimal MontoTransferencia { get; set; }
    public decimal? MontoRecibido { get; set; }
    public decimal? Cambio { get; set; }
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
    public decimal MontoEfectivo { get; set; }
    public decimal MontoTransferencia { get; set; }
    public decimal? MontoRecibido { get; set; }
    public decimal? Cambio { get; set; }
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
    /// <summary>Billetes que entregó el cliente, tal cual los tipeó el cajero. El backend calcula lo cobrado, lo recibido y el cambio. En Efectivo: null/0 = pago justo.</summary>
    public decimal? MontoEfectivo { get; set; }
    /// <summary>Monto transferido. En TransferenciaQR: null/0 = total a cobrar.</summary>
    public decimal? MontoTransferencia { get; set; }
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
    public TipoPrecioVenta? TipoPrecio { get; set; }
}

public class CrearVentaDetalleDto
{
    public int VarianteId { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioUnitarioCobrado { get; set; }
    /// <summary>Opcional. Qué campo de precio (Unitario o Mayoreo) usó el cajero para esta línea. Si no viene, se guarda null — no se rechaza la venta.</summary>
    public TipoPrecioVenta? TipoPrecio { get; set; }
}
