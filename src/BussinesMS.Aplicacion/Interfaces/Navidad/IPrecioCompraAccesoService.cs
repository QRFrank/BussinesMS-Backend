namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Permiso "Ver precios de compra" del sistema Navideño: el admin siempre; el resto según
// UsuarioSistema.VerPreciosCompra (SistemaId = 2). Se resuelve una vez por request.
public interface IPrecioCompraAccesoService
{
    Task<bool> PuedeVerPreciosCompraAsync();
}
