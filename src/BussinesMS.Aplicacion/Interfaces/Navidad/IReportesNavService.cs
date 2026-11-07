using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Reportes del sistema navideño (módulo 9): solo admin (403 SIN_PERMISO), solo lectura. Sin temporadaId, la temporada abierta; con temporadaId, también una cerrada.
// Los totales que también muestra el dashboard salen de ResumenNavCalculos (el mismo método).
public interface IReportesNavService
{
    Task<ReporteResultadoNavDto> ObtenerResultadoAsync(DateTime? desde, DateTime? hasta, int? temporadaId = null);

    Task<ReporteVentasNavDto> ObtenerVentasAsync(ReporteVentasNavFiltrosDto filtros);

    Task<ReporteStockNavDto> ObtenerStockAsync(int? almacenId, int? proveedorId, bool incluirSinStock, int? temporadaId = null);

    Task<ReporteFaltantesNavDto> ObtenerFaltantesAsync(int? proveedorId, int? codigoClienteId, int? temporadaId = null);

    Task<ReporteRecepcionesCodigoNavDto> ObtenerRecepcionesCodigoAsync(int proveedorId, int? codigoClienteId, DateTime? desde, DateTime? hasta, int? temporadaId = null);

    Task<ReporteEntradasProductoNavDto> ObtenerEntradasProductoAsync(int productoId, int? almacenId, DateTime? desde, DateTime? hasta, int? temporadaId = null);

    Task<ReporteDeudasNavDto> ObtenerDeudasAsync(int? temporadaId = null);

    Task<ReporteGastosNavDto> ObtenerGastosAsync(DateTime? desde, DateTime? hasta, int? categoriaId, int? temporadaId = null);

    Task<ReportePagosProveedoresNavDto> ObtenerPagosProveedoresAsync(DateTime? desde, DateTime? hasta, int? proveedorId, int? codigoClienteId, int? temporadaId = null);
}
