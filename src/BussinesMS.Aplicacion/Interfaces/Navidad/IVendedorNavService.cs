using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IVendedorNavService
{
    Task<PagedResultDto<VendedorNavDto>> ObtenerTodosAsync(VendedorFiltroDto query);
    Task<VendedorNavDto?> ObtenerPorIdAsync(int id);
    Task<List<UsuarioDisponibleNavDto>> ObtenerUsuariosDisponiblesAsync();
    Task<VendedorNavDto> CrearAsync(CrearVendedorNavDto dto);
    Task<VendedorNavDto> ActualizarAsync(ActualizarVendedorNavDto dto);
    Task EliminarAsync(int id);
}
