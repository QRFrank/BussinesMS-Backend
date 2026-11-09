using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Comprobante de entrega de una reserva: solo productos y unidades entregadas (sin precios, saldo ni pendientes)
public class ComprobanteEntregaDatosService : IComprobanteEntregaDatosService
{
    private readonly IEntregaVentaNavRepository _entregas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IAlmacenRepository _almacenes;

    public ComprobanteEntregaDatosService(IEntregaVentaNavRepository entregas, IUsuarioRepository usuarios, IAlmacenRepository almacenes)
    {
        _entregas = entregas;
        _usuarios = usuarios;
        _almacenes = almacenes;
    }

    public async Task<ComprobanteEntregaDatos?> ObtenerAsync(int entregaId)
    {
        var e = await _entregas.AsQueryable()
            .AsNoTracking()
            .Include(x => x.Venta).ThenInclude(v => v!.Cliente)
            .Include(x => x.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(x => x.Id == entregaId);
        if (e == null || !e.IsActive || e.Venta == null || !e.Venta.IsActive) return null;

        var almacen = await _almacenes.AsQueryable().AsNoTracking()
            .Where(a => a.Id == e.AlmacenId).Select(a => a.Nombre).FirstOrDefaultAsync() ?? string.Empty;

        var entrego = string.Empty;
        var u = (await _usuarios.ObtenerPorIdsAsync(new[] { e.UsuarioId })).FirstOrDefault();
        if (u != null) entrego = $"{u.Nombre?.Trim()} {u.Apellido?.Trim()}".Trim();

        return new ComprobanteEntregaDatos
        {
            EntregaId = e.Id,
            NroVenta = e.Venta.NroVenta,
            Anulada = e.Anulada || e.Venta.Anulada || e.Venta.Tipo != TipoVentaNav.Reserva,
            Almacen = almacen,
            FechaLocal = BoliviaTimeZone.ToLocal(e.CreatedAt),
            Cliente = e.Venta.Cliente?.Nombre,
            Entrego = entrego,
            // Se omiten los renglones que quedaron en 0 por una baja de cantidad
            Lineas = e.Detalles.Where(d => d.Unidades > 0).OrderBy(d => d.Id)
                .Select(d => new ComprobanteEntregaLineaNav(
                    d.Producto?.Nombre ?? d.Producto?.Descripcion ?? "Producto",
                    d.Unidades,
                    MensajesAvisosNav.Cantidad(d.Unidades, d.Producto?.UnidadesPorEmpaque, d.Producto?.NombreEmpaque)))
                .ToList()
        };
    }
}
