using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class DevolucionClienteRepository : IDevolucionClienteRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DevolucionClienteRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<DevolucionCliente> AsQueryable()
        => _context.DevolucionesClientes.AsQueryable();

    public async Task<DevolucionCliente?> ObtenerPorIdAsync(int id)
        => await _context.DevolucionesClientes.FindAsync(id);

    public async Task<DevolucionCliente?> ObtenerConDetallesAsync(int id)
        => await _context.DevolucionesClientes
            .Include(d => d.LoteAlmacenOrigen)
            .Include(d => d.LoteAlmacenDevuelto)
            .Include(d => d.Variante)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<DevolucionCliente> CrearAsync(DevolucionCliente entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.UsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.DevolucionesClientes.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<DevolucionCliente> ActualizarAsync(DevolucionCliente entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.DevolucionesClientes.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }
}
