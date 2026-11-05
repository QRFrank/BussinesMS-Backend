using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IWhatsAppBandejaAdminService
{
    Task<PagedResultDto<WhatsAppEnvioDto>> ObtenerBitacoraAsync(WhatsAppBitacoraFiltroDto filtro);
    Task<WhatsAppEnvioDto> ObtenerEnvioAsync(int id);
    Task<WhatsAppEnvioDto> ReintentarAsync(int id);
    Task<WhatsAppEstadoBannerDto> ObtenerEstadoBannerAsync();
    Task<WhatsAppPruebaBandejaDto> EnviarPruebaPorBandejaAsync(EnviarPruebaBandejaDto dto);
}
