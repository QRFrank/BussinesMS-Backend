using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class ClienteNavRepository : NavidadRepositorioBase<ClienteNav>, IClienteNavRepository
{
    public ClienteNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<bool> ExisteDocumentoAsync(string documento, int? excluirId = null)
    {
        var doc = documento.Trim().ToLower();
        return await _dbSet.AnyAsync(c => c.IsActive
            && c.Documento != null
            && c.Documento.ToLower() == doc
            && (!excluirId.HasValue || c.Id != excluirId.Value));
    }
}
