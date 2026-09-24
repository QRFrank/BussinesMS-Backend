using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface ISesionCajaRepository
{
    Task<SesionCaja?> ObtenerPorIdAsync(int id);
    Task<SesionCaja?> ObtenerAbiertaPorUsuarioAsync(int usuarioId, int almacenId);
    Task<List<SesionCaja>> ObtenerTodasAsync();
    Task<SesionCaja> CrearAsync(SesionCaja entidad);
    Task<SesionCaja> CrearSinGuardarAsync(SesionCaja entidad);
    Task<SesionCaja> ActualizarAsync(SesionCaja entidad);
}
