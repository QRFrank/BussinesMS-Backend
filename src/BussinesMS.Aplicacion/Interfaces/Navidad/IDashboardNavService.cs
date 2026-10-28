using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Dashboard navideño por rol: solo lectura sobre la temporada abierta
public interface IDashboardNavService
{
    Task<DashboardPerfilNavDto> ObtenerPerfilAsync();

    // Solo rol Admin (403 SIN_PERMISO si no)
    Task<DashboardAdminNavDto> ObtenerAdminAsync();

    // Datos del vendedor vinculado al usuario actual; almacenId = tienda de sesión (avisos de conteo y caja)
    Task<DashboardVendedorNavDto> ObtenerVendedorAsync(int? almacenId);

    // Solo rol EncargadoAlmacen: únicamente sus almacenes de la temporada abierta
    Task<DashboardEncargadoNavDto> ObtenerEncargadoAsync(int? umbralStockBajo);
}
