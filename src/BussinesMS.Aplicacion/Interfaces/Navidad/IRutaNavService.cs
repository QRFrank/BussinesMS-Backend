using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Módulo Ruta: salidas con cargas y devoluciones, cierre (crea la venta de ruta), abonos del vendedor, deuda y edición de ventas de ruta
public interface IRutaNavService
{
    Task<RutaContextoDto> ObtenerContextoAsync();

    // Salidas
    Task<PagedResultDto<SalidaRutaListaDto>> ObtenerSalidasAsync(SalidaRutaFiltroDto query);
    Task<SalidaRutaDto?> ObtenerSalidaAsync(int id);
    Task<SalidaRutaDto> CargarAsync(CargarSalidaRutaDto dto);
    Task<SalidaRutaDto> DevolverAsync(int salidaId, DevolverSalidaRutaDto dto);
    Task<SalidaRutaDto> CerrarAsync(int salidaId, CerrarSalidaRutaDto dto);

    // Anulaciones (solo admin)
    Task<SalidaRutaDto> AnularCargaAsync(int cargaId);
    Task<SalidaRutaDto> AnularDevolucionAsync(int devolucionId);
    Task<SalidaRutaDto> AnularCierreAsync(int salidaId);
    Task<SalidaRutaDto> AnularSalidaAsync(int salidaId);

    // Abonos y deuda
    Task<List<DeudaVendedorRutaDto>> ObtenerDeudasAsync(int? vendedorId);
    Task<PagedResultDto<AbonoVendedorDto>> ObtenerAbonosAsync(AbonoVendedorFiltroDto query);
    Task<AbonoVendedorDto?> ObtenerAbonoAsync(int id);
    Task<AbonoVendedorDto> RegistrarAbonoAsync(CrearAbonoVendedorDto dto);
    Task<AbonoVendedorDto> AnularAbonoAsync(int id);

    // Editar venta de ruta (solo admin)
    Task<VentaNavDto> EditarPreciosVentaAsync(int ventaId, EditarPreciosVentaRutaDto dto);
    Task<VentaNavDto> DevolverVentaAsync(int ventaId, DevolverVentaRutaDto dto);
}
