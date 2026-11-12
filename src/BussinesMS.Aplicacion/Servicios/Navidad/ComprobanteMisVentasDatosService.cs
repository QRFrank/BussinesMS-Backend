using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// C7: datos del comprobante de una venta de "Mis ventas" (sin precios de compra ni costos)
public class ComprobanteMisVentasDatosService : IComprobanteMisVentasDatosService
{
    private readonly IVentaClienteRutaNavRepository _ventas;
    private readonly IUsuarioRepository _usuarios;

    public ComprobanteMisVentasDatosService(IVentaClienteRutaNavRepository ventas, IUsuarioRepository usuarios)
    {
        _ventas = ventas;
        _usuarios = usuarios;
    }

    public async Task<ComprobanteVentaDatos?> ObtenerAsync(int ventaId)
    {
        var v = await _ventas.ObtenerConDetallesAsync(ventaId);
        if (v == null || !v.IsActive) return null;

        var vendedor = string.Empty;
        if (v.Vendedor != null)
        {
            var u = (await _usuarios.ObtenerPorIdsAsync(new[] { v.Vendedor.UsuarioId })).FirstOrDefault();
            if (u != null) vendedor = $"{u.Nombre?.Trim()} {u.Apellido?.Trim()}".Trim();
        }

        return new ComprobanteVentaDatos
        {
            VentaId = v.Id,
            // "Mis ventas" no tiene número correlativo propio: el N° del comprobante es el Id del registro
            NroVenta = v.Id,
            EsVentaRuta = true,
            Anulada = v.Anulada,
            FechaLocal = v.Fecha,
            Cliente = v.Cliente?.Nombre,
            Vendedor = vendedor,
            Lineas = v.Detalles.Where(d => d.IsActive && d.Unidades > 0).OrderBy(d => d.Id)
                .Select(d => new ComprobanteLineaNav(d.Producto != null ? VentaNavService.NombreMostrar(d.Producto) : "Producto", d.Unidades, d.PrecioUnitario, d.Subtotal,
                    MensajesAvisosNav.Equivalencia(d.Unidades, d.Producto?.UnidadesPorEmpaque, d.Producto?.NombreEmpaque)))
                .ToList(),
            Total = v.Total,
            Descuento = 0,
            Neto = v.Total,
            Cobrado = v.Pagado,
            Saldo = v.Saldo,
            Pagos = v.Cobros.Where(c => c.IsActive && !c.Anulado).OrderBy(c => c.Fecha).ThenBy(c => c.Id)
                .Select(c => new ComprobantePagoNav(c.Medio?.ToString() ?? "", c.Monto)).ToList()
        };
    }
}
