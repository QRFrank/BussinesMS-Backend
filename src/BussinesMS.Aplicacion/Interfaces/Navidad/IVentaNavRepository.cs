using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IVentaNavRepository : IRepositorio<VentaNav>
{
    // Sin tracking: cliente, vendedor, líneas con producto, entregas con detalle y cobros
    Task<VentaNav?> ObtenerConDetallesAsync(int id);

    // CON tracking: líneas, entregas con detalle y cobros (para editar, entregar, cobrar y anular)
    Task<VentaNav?> ObtenerParaEdicionAsync(int id);

    // Máximo NroVenta de la temporada + 1
    Task<int> ObtenerSiguienteNroAsync(int temporadaId);
}

public interface IEntregaVentaNavRepository : IRepositorio<EntregaVenta>
{
}

public interface ICobroVentaNavRepository : IRepositorio<CobroVenta>
{
}

public interface IConsumoNavRepository : IRepositorio<ConsumoNav>
{
    // Sin tracking: detalles con producto
    Task<ConsumoNav?> ObtenerConDetallesAsync(int id);
}
