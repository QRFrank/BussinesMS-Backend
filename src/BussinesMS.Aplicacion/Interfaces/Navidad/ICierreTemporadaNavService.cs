using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Cierre de temporada (2026-11-07): solo admin (403 SIN_PERMISO)
public interface ICierreTemporadaNavService
{
    // Pendientes (bloquean o alertan) y resumen del drawer. El mismo cálculo que usa el cierre
    Task<CierreTemporadaResumenNavDto> ObtenerResumenAsync(int temporadaId);

    // 409 TEMPORADA_CERRADA si ya está cerrada; 409 TEMPORADA_CON_PENDIENTES si hay bloqueantes; 400 CONFIRMACION_INVALIDA si el nombre no coincide.
    // En una transacción: stock sobrante a pérdida (kardex "Cierre de temporada"), foto del resultado y paso a Cerrada
    Task<CierreTemporadaResultadoNavDto> CerrarAsync(int temporadaId, CerrarTemporadaNavDto? dto);

    // Foto guardada al cerrar. 404 si la temporada no tiene cierre
    Task<CierreTemporadaFotoNavDto> ObtenerCierreAsync(int temporadaId);
}
