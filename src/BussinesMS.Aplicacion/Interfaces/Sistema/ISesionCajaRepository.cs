using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface ISesionCajaRepository
{
    IQueryable<SesionCaja> AsQueryable();
    Task<SesionCaja?> ObtenerPorIdAsync(int id);
    Task<SesionCaja?> ObtenerAbiertaPorUsuarioAsync(int usuarioId, int almacenId);
    Task<List<SesionCaja>> ObtenerTodasAsync();
    Task<SesionCaja> CrearAsync(SesionCaja entidad);
    Task<SesionCaja> CrearSinGuardarAsync(SesionCaja entidad);
    Task<SesionCaja> ActualizarAsync(SesionCaja entidad);
    Task<Dictionary<int, (decimal EgresosGastos, decimal EgresosPagoProveedor)>> CalcularEgresosAsync(IEnumerable<int> sesionCajaIds);
    Task<Dictionary<int, int>> ContarTransferenciasAsync(IEnumerable<int> sesionCajaIds);
}
