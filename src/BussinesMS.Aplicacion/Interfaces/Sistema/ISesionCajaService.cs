using BussinesMS.Aplicacion.DTOs.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface ISesionCajaService
{
    Task<SesionCajaDto> AbrirCajaAsync(CrearSesionCajaDto dto);
    Task<SesionCajaDto> CerrarCajaAsync(int id, CerrarSesionCajaDto dto);
    Task<SesionCajaDto?> ObtenerPorIdAsync(int id);
    Task<SesionCajaDto?> ObtenerAbiertaAsync(int usuarioId, int almacenId);
}
