using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Módulo 8: reporte de ventas por vendedor y pagos a vendedores (sueldo y comisión)
public interface IPagoVendedorNavService
{
    Task<PagoVendedorContextoDto> ObtenerContextoAsync();
    Task<List<ReporteVentasVendedorDto>> ObtenerReporteAsync(ReporteVentasVendedorFiltroDto filtro);
    Task<PagedResultDto<PagoVendedorDto>> ObtenerPagosAsync(PagoVendedorFiltroDto query);
    Task<PagoVendedorDto?> ObtenerPagoAsync(int id);
    Task<PagoVendedorDto> RegistrarAsync(RegistrarPagoVendedorDto dto);
    Task<PagoVendedorDto> AnularAsync(int id);
    Task<List<SueldoVendedorResumenDto>> ObtenerResumenSueldosAsync(int? temporadaId, int? vendedorId);
}
