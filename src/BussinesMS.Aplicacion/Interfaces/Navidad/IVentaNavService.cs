using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IVentaNavService
{
    Task<PagedResultDto<VentaNavDto>> ObtenerTodosAsync(VentaNavFiltroDto query);
    // "Mis ventas" de tienda: canal Tienda y vendedor = el vinculado al usuario logueado (se ignora el vendedor/canal del query)
    Task<(PagedResultDto<VentaNavDto> Resultado, bool TieneVendedor)> ObtenerMiasAsync(VentaNavFiltroDto query);
    Task<VentaNavDto?> ObtenerPorIdAsync(int id);

    // {esAdmin, vendedorActual?} del usuario logueado en la temporada abierta
    Task<VentaContextoDto> ObtenerContextoAsync();

    // Productos activos con precios y stock/bloqueado/disponible en el almacén
    Task<List<VentaProductoNavDto>> ObtenerProductosAsync(int almacenId);

    Task<VentaNavDto> CrearAsync(CrearVentaNavDto dto);
    // Solo para el cierre de conteo (venta del faltante "Venta no registrada"): usa la transacción ya abierta por quien llama
    Task<VentaNavDto> CrearDesdeCierreAsync(CrearVentaNavDto dto);
    Task<VentaNavDto> EditarAsync(int id, EditarVentaNavDto dto);

    Task<VentaNavDto> AnularEntregaAsync(int entregaId);
    Task<VentaNavDto> RegistrarCobroAsync(int ventaId, RegistrarCobroVentaDto dto);
    Task<VentaNavDto> AnularCobroAsync(int cobroId);
    Task AnularAsync(int id);
}
