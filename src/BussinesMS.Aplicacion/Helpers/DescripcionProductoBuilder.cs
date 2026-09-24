using System.Text;

namespace BussinesMS.Aplicacion.Helpers;

public static class DescripcionProductoBuilder
{
    public static string Construir(
        string nombreProducto, string sabor, string pesoTamanio,
        int? cantidadCaja, string? fabricante = null)
    {
        var sb = new StringBuilder();
        sb.Append($"{nombreProducto} {sabor} de {pesoTamanio}");
        if (cantidadCaja.HasValue && cantidadCaja.Value > 0)
            sb.Append($" x{cantidadCaja}");
        if (!string.IsNullOrWhiteSpace(fabricante))
            sb.Append($" - {fabricante}");
        return sb.ToString().Trim();
    }
}
