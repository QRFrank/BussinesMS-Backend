using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Infraestructura.Persistencia;
using BussinesMS.Infraestructura.Repositorios.Compartido;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Auth;

public class WhatsAppInstanciaRepository : RepositorioBase<WhatsAppInstancia>, IWhatsAppInstanciaRepository
{
    public WhatsAppInstanciaRepository(AuthDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<WhatsAppInstancia?> ObtenerVigenteAsync()
    {
        return await _dbSet.FirstOrDefaultAsync(w => w.IsActive);
    }
}
