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
    Task<MisVentasMeDebenDto> ObtenerMeDebenAsync(int? temporadaId);
    Task<MisVentasTotalSalidaDto> ObtenerTotalRegistradoSalidaAsync(int salidaId);
}
