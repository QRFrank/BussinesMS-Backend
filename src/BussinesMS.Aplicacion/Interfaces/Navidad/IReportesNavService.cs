using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Reportes del sistema navideño (módulo 9): solo admin (403 SIN_PERMISO), solo lectura, temporada abierta.
// Los totales que también muestra el dashboard salen de ResumenNavCalculos (el mismo método).
public interface IReportesNavService
{
    Task<ReporteResultadoNavDto> ObtenerResultadoAsync(DateTime? desde, DateTime? hasta);

    Task<ReporteVentasNavDto> ObtenerVentasAsync(ReporteVentasNavFiltrosDto filtros);

    Task<ReporteStockNavDto> ObtenerStockAsync(int? almacenId, int? proveedorId, bool incluirSinStock);

    Task<ReporteFaltantesNavDto> ObtenerFaltantesAsync(int? proveedorId, int? codigoClienteId);

    Task<ReporteRecepcionesCodigoNavDto> ObtenerRecepcionesCodigoAsync(int proveedorId, int? codigoClienteId, DateTime? desde, DateTime? hasta);

    Task<ReporteEntradasProductoNavDto> ObtenerEntradasProductoAsync(int productoId, int? almacenId, DateTime? desde, DateTime? hasta);

    Task<ReporteDeudasNavDto> ObtenerDeudasAsync();

    Task<ReporteGastosNavDto> ObtenerGastosAsync(DateTime? desde, DateTime? hasta, int? categoriaId);

    Task<ReportePagosProveedoresNavDto> ObtenerPagosProveedoresAsync(DateTime? desde, DateTime? hasta, int? proveedorId, int? codigoClienteId);
}
