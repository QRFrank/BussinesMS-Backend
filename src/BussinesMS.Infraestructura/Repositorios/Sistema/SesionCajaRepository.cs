using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class SesionCajaRepository : ISesionCajaRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SesionCajaRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<SesionCaja?> ObtenerPorIdAsync(int id)
        => await _context.SesionesCaja
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<SesionCaja?> ObtenerAbiertaPorUsuarioAsync(int usuarioId, int almacenId)
        => await _context.SesionesCaja
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId
                && x.AlmacenId == almacenId
                && x.Estado == EstadoSesionCaja.Abierta);

    public async Task<List<SesionCaja>> ObtenerTodasAsync()
        => await _context.SesionesCaja
            .OrderByDescending(x => x.FechaApertura)
            .ToListAsync();

    public async Task<SesionCaja> CrearAsync(SesionCaja entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.UsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.SesionesCaja.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<SesionCaja> CrearSinGuardarAsync(SesionCaja entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.UsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.SesionesCaja.Add(entidad);
        return entidad;
    }

    public async Task<SesionCaja> ActualizarAsync(SesionCaja entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.SesionesCaja.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }
}
