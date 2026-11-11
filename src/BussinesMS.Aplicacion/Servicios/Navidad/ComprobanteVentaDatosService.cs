using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

public class ComprobanteVentaDatosService : IComprobanteVentaDatosService
{
    private readonly IVentaNavRepository _ventas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IAlmacenRepository _almacenes;

    public ComprobanteVentaDatosService(IVentaNavRepository ventas, IUsuarioRepository usuarios, IAlmacenRepository almacenes)
    {
        _ventas = ventas;
        _usuarios = usuarios;
        _almacenes = almacenes;
    }

    public async Task<ComprobanteVentaDatos?> ObtenerAsync(int ventaId)
    {
        var v = await _ventas.ObtenerConDetallesAsync(ventaId);
        if (v == null || !v.IsActive) return null;

        var tienda = string.Empty;
        if (v.AlmacenId.HasValue)
            tienda = await _almacenes.AsQueryable().AsNoTracking()
                .Where(a => a.Id == v.AlmacenId.Value).Select(a => a.Nombre).FirstOrDefaultAsync() ?? string.Empty;

        var vendedor = string.Empty;
        if (v.Vendedor != null)
        {
            var u = (await _usuarios.ObtenerPorIdsAsync(new[] { v.Vendedor.UsuarioId })).FirstOrDefault();
            if (u != null) vendedor = $"{u.Nombre?.Trim()} {u.Apellido?.Trim()}".Trim();
        }

        return new ComprobanteVentaDatos
        {
            VentaId = v.Id,
            NroVenta = v.NroVenta,
            EsReserva = v.Tipo == BussinesMS.Dominio.Enums.Navidad.TipoVentaNav.Reserva,
            Anulada = v.Anulada,
            Tienda = tienda,
            FechaLocal = BoliviaTimeZone.ToLocal(v.CreatedAt),
            Cliente = v.Cliente?.Nombre,
            Vendedor = vendedor,
            Lineas = v.Detalles.Where(d => d.Unidades > 0).OrderBy(d => d.Id)
                .Select(d => new ComprobanteLineaNav(d.Producto?.Nombre ?? d.Producto?.Descripcion ?? "Producto", d.Unidades, d.PrecioUnitario, d.Subtotal))
                .ToList(),
            Total = v.MontoTotal,
            Descuento = v.Descuento,
            Neto = v.MontoNeto,
            Cobrado = v.MontoCobrado,
            Saldo = v.MontoSaldo,
            Pagos = v.Cobros.Where(c => !c.Anulado).OrderBy(c => c.Id)
                .Select(c => new ComprobantePagoNav(c.Medio.ToString(), c.Monto, c.Fecha)).ToList()
        };
    }
}
