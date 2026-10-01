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

    public IQueryable<SesionCaja> AsQueryable()
        => _context.SesionesCaja.AsQueryable();

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

    public async Task<Dictionary<int, (decimal EgresosGastos, decimal EgresosPagoProveedor)>> CalcularEgresosAsync(IEnumerable<int> sesionCajaIds)
    {
        var ids = sesionCajaIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, (decimal EgresosGastos, decimal EgresosPagoProveedor)>();

        var gastos = await _context.GastosOperativos
            .Where(g => g.IsActive && g.SesionCajaId.HasValue && ids.Contains(g.SesionCajaId.Value))
            .GroupBy(g => g.SesionCajaId!.Value)
            .Select(g => new { SesionCajaId = g.Key, Total = g.Sum(x => x.MontoCaja) })
            .ToDictionaryAsync(x => x.SesionCajaId, x => x.Total);

        var pagos = await _context.PagosCompra
            .Where(p => p.IsActive && p.SesionCajaId.HasValue && ids.Contains(p.SesionCajaId.Value))
            .GroupBy(p => p.SesionCajaId!.Value)
            .Select(g => new { SesionCajaId = g.Key, Total = g.Sum(x => x.MontoCaja) })
            .ToDictionaryAsync(x => x.SesionCajaId, x => x.Total);

        return ids.ToDictionary(
            id => id,
            id => (gastos.GetValueOrDefault(id), pagos.GetValueOrDefault(id)));
    }

    /// <summary>
    /// Cantidad de ventas activas (no anuladas) con MontoTransferencia > 0 por sesión (QR/Transferencia y Mixtas).
    /// </summary>
    public async Task<Dictionary<int, int>> ContarTransferenciasAsync(IEnumerable<int> sesionCajaIds)
    {
        var ids = sesionCajaIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, int>();

        return await _context.Ventas
            .Where(v => v.IsActive && v.MontoTransferencia > 0 && ids.Contains(v.SesionCajaId))
            .GroupBy(v => v.SesionCajaId)
            .Select(g => new { SesionCajaId = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.SesionCajaId, x => x.Cantidad);
    }
}
