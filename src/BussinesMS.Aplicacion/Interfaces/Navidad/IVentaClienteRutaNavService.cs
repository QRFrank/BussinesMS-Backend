using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Registro personal y privado de ventas del vendedor de ruta (solo el vendedor dueño; el admin recibe 403)
public interface IVentaClienteRutaNavService
{
    Task<PagedResultDto<VentaClienteRutaListaDto>> ObtenerAsync(MisVentasRutaFiltroDto query);
    Task<VentaClienteRutaDto> ObtenerPorIdAsync(int id);
    Task<VentaClienteRutaDto> RegistrarAsync(RegistrarVentaClienteRutaDto dto);
    Task<VentaClienteRutaDto> EditarAsync(int id, EditarVentaClienteRutaDto dto);
    Task<VentaClienteRutaDto> RegistrarCobroAsync(int id, RegistrarCobroClienteRutaDto dto);
    Task<VentaClienteRutaDto> AnularCobroAsync(int id, int cobroId);
    Task<VentaClienteRutaDto> AnularAsync(int id);
    // C7: encola un comprobante PDF nuevo por WhatsApp (solo el vendedor dueño; 400 sin/inválido, 404, 409 si está anulada)
    Task<ComprobanteEncoladoDto> ReenviarComprobanteAsync(int id, string? telefono);
    Task<MisVentasMeDebenDto> ObtenerMeDebenAsync(int? temporadaId);
    Task<MisVentasTotalSalidaDto> ObtenerTotalRegistradoSalidaAsync(int salidaId);
    // Cargado/devuelto/en poder/registrado por producto de la salida (la abierta del vendedor si no se indica; null si no tiene)
    Task<MisVentasSalidaResumenDto?> ObtenerResumenSalidaAsync(int? salidaId, int? excluirVentaId);
}
