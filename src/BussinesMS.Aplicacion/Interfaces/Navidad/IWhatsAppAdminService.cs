using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IWhatsAppAdminService
{
    Task<WhatsAppNumeroDto> ObtenerNumeroAsync();
    Task<WhatsAppQrDto> ObtenerQrAsync();
    Task<WhatsAppNumeroDto> DesconectarAsync();
    Task<WhatsAppQrDto> CambiarNumeroAsync();
    Task<WhatsAppPruebaDto> EnviarPruebaAsync(EnviarPruebaWhatsAppDto dto);
}
