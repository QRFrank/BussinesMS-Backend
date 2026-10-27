using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ISalidaRutaNavRepository : IRepositorio<SalidaRuta>
{
    // CON tracking: cargas con detalles y devoluciones con detalles
    Task<SalidaRuta?> ObtenerParaEdicionAsync(int id);

    // Sin tracking: vendedor, cargas con detalles y devoluciones con detalles
    Task<SalidaRuta?> ObtenerConDetallesAsync(int id);
}

public interface ISalidaRutaCargaNavRepository : IRepositorio<SalidaRutaCarga>
{
}

public interface IDevolucionRutaNavRepository : IRepositorio<DevolucionRuta>
{
}

public interface IAbonoVendedorNavRepository : IRepositorio<AbonoVendedor>
{
    // CON tracking: aplicaciones
    Task<AbonoVendedor?> ObtenerParaEdicionAsync(int id);

    // Sin tracking: vendedor y aplicaciones
    Task<AbonoVendedor?> ObtenerConDetallesAsync(int id);
}
