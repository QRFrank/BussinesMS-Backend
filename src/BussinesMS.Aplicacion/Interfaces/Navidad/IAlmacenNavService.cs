using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IAlmacenNavService
{
    Task<List<AlmacenNavDto>> ObtenerTodosAsync(AlmacenNavFiltroDto query);
    Task<List<EncargadoNavDto>> ObtenerEncargadosAsync(int almacenId);
    // Solo admin, temporada abierta
    Task<List<EncargadoNavDto>> ReemplazarEncargadosAsync(int almacenId, AsignarEncargadosNavDto dto);
    // Usuarios activos con rol EncargadoAlmacen y acceso al sistema Navideño
    Task<List<EncargadoNavDto>> ObtenerCandidatosAsync();
}
