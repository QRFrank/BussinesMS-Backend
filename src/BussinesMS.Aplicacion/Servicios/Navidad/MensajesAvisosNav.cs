using System.Globalization;
using System.Text;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

/// <summary>Producto de una línea de aviso, con el empaque para mostrar javas/cajas + unidades.</summary>
public sealed record LineaAviso(string Nombre, int Unidades, int? UnidadesPorEmpaque = null, string? NombreEmpaque = null);

/// <summary>Diferencia de un conteo de tienda (apertura o cierre).</summary>
public sealed record DiferenciaConteoAviso(string Nombre, int Sistema, int Contado, int Diferencia);

/// <summary>
/// Textos de los avisos internos de WhatsApp (fase 3). Funciones puras: sin acceso a datos.
/// Reglas: nunca precios de compra ni costos; productos agrupados en un solo mensaje (hasta 15 líneas y "y N más").
/// </summary>
public static class MensajesAvisosNav
{
    /// <summary>Firma al pie de los mensajes (decisión 8.13). La pantalla para editarla es de la fase 5: por ahora es una constante.</summary>
    public const string Firma = "Panetones 2026";

    public const int MaxLineas = 15;

    private static readonly NumberFormatInfo FormatoBs = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = new[] { 3 }
    };

    // ---------- Helpers de formato ----------

    public static string Bs(decimal monto) => "Bs " + monto.ToString("N2", FormatoBs);

    public static string Fecha(DateTime local) => local.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string Dia(DateTime local) => local.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string Medio(string? medio) => medio?.ToLowerInvariant() ?? "";

    /// <summary>"120 u (10 javas)" / "125 u (10 javas + 5 u)" / "5 u" si el producto no tiene empaque.</summary>
    public static string Cantidad(int unidades, int? unidadesPorEmpaque, string? nombreEmpaque)
    {
        if (unidadesPorEmpaque is > 1 && !string.IsNullOrWhiteSpace(nombreEmpaque))
        {
            var completos = unidades / unidadesPorEmpaque.Value;
            var sueltas = unidades % unidadesPorEmpaque.Value;
            if (completos > 0)
            {
                var nombre = nombreEmpaque.Trim().ToLowerInvariant();
                if (completos != 1 && !nombre.EndsWith('s')) nombre += "s";
                return sueltas == 0
                    ? $"{unidades} u ({completos} {nombre})"
                    : $"{unidades} u ({completos} {nombre} + {sueltas} u)";
            }
        }
        return $"{unidades} u";
    }

    /// <summary>Líneas "• Producto: cantidad" (máx. 15) y "• y N más" si hay más.</summary>
    public static void AgregarProductos(StringBuilder sb, IEnumerable<LineaAviso> lineas)
    {
        var lista = lineas.Where(l => l.Unidades != 0).OrderBy(l => l.Nombre).ToList();
        foreach (var l in lista.Take(MaxLineas))
            sb.Append("• ").Append(l.Nombre).Append(": ").AppendLine(Cantidad(l.Unidades, l.UnidadesPorEmpaque, l.NombreEmpaque));
        if (lista.Count > MaxLineas)
            sb.Append("• y ").Append(lista.Count - MaxLineas).AppendLine(" más");
    }

    /// <summary>Junta las líneas repetidas del mismo producto.</summary>
    public static List<LineaAviso> Agrupar(IEnumerable<LineaAviso> lineas)
        => lineas.GroupBy(l => l.Nombre)
            .Select(g => g.First() with { Unidades = g.Sum(x => x.Unidades) })
            .ToList();

    private static string Cerrar(StringBuilder sb)
    {
        sb.Append("— ").Append(Firma);
        return sb.ToString();
    }

    // ---------- Tienda y caja (decisión 8.8) ----------

    public static string TiendaAbiertaOCerrada(bool apertura, string almacen, DateTime fechaHora, string usuario,
        DateTime? diaPendiente, int productos, int totalContado, IReadOnlyList<DiferenciaConteoAviso> diferencias)
    {
        var sb = new StringBuilder();
        sb.AppendLine(apertura ? $"🏪 Tienda abierta — {almacen}" : $"🔒 Tienda cerrada — {almacen}");
        sb.Append(Fecha(fechaHora)).Append(" · ").Append(apertura ? "Abrió: " : "Cerró: ").AppendLine(usuario);
        if (diaPendiente.HasValue)
            sb.AppendLine($"Cierre atrasado del día {Dia(diaPendiente.Value)}");
        sb.AppendLine($"Contó {productos} producto(s) · {totalContado} u en total");
        if (diferencias.Count == 0)
            sb.AppendLine("Sin diferencias con el sistema");
        else
        {
            var sobran = diferencias.Where(d => d.Diferencia > 0).Sum(d => d.Diferencia);
            var faltan = -diferencias.Where(d => d.Diferencia < 0).Sum(d => d.Diferencia);
            sb.AppendLine($"⚠️ Diferencias: {diferencias.Count} producto(s) · sobran {sobran} u · faltan {faltan} u");
            foreach (var d in diferencias.OrderBy(d => d.Nombre).Take(MaxLineas))
                sb.AppendLine($"• {d.Nombre}: contó {d.Contado}, sistema {d.Sistema} ({(d.Diferencia > 0 ? "+" : "")}{d.Diferencia})");
            if (diferencias.Count > MaxLineas)
                sb.AppendLine($"• y {diferencias.Count - MaxLineas} más");
        }
        return Cerrar(sb);
    }

    public static string CierreCaja(string almacen, DateTime desde, DateTime hasta, string usuario, decimal totalVendido,
        decimal efectivoEsperado, decimal efectivoEntregado, decimal diferencia, decimal transferencias, decimal credito, string? observacion)
    {
        var sb = new StringBuilder();
        sb.AppendLine(diferencia != 0 ? $"⚠️ Cierre de caja CON DIFERENCIA — {almacen}" : $"💰 Cierre de caja — {almacen}");
        sb.AppendLine($"Turno {Fecha(desde)} → {Fecha(hasta)} · Registró: {usuario}");
        sb.AppendLine($"Vendido: {Bs(totalVendido)} · Efectivo esperado: {Bs(efectivoEsperado)}");
        if (diferencia == 0)
            sb.AppendLine($"Entregado: {Bs(efectivoEntregado)} → sin diferencia");
        else if (diferencia < 0)
            sb.AppendLine($"Entregado: {Bs(efectivoEntregado)} → *FALTA {Bs(-diferencia)}*");
        else
            sb.AppendLine($"Entregado: {Bs(efectivoEntregado)} → *SOBRA {Bs(diferencia)}*");
        sb.AppendLine($"Transferencias: {Bs(transferencias)} (no se entregan)");
        if (credito > 0) sb.AppendLine($"A crédito: {Bs(credito)}");
        if (!string.IsNullOrWhiteSpace(observacion)) sb.AppendLine($"Obs.: \"{observacion}\"");
        return Cerrar(sb);
    }

    // ---------- Recepciones (A3 / E1) ----------

    public static string RecepcionAdmin(int recepcionId, DateTime fecha, string proveedor, string? nroFactura, string usuario,
        IReadOnlyList<(string Almacen, int Unidades)> porAlmacen, IEnumerable<LineaAviso> productos)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📦 Recepción N° {recepcionId} — {Dia(fecha)}");
        sb.Append("Proveedor: ").Append(proveedor);
        if (!string.IsNullOrWhiteSpace(nroFactura)) sb.Append(" · Factura ").Append(nroFactura);
        sb.AppendLine();
        sb.AppendLine(string.Join(" · ", porAlmacen.Select(a => $"{a.Almacen}: {a.Unidades} u")));
        AgregarProductos(sb, productos);
        sb.AppendLine($"Total: {porAlmacen.Sum(a => a.Unidades)} u");
        sb.AppendLine($"Registró: {usuario}");
        return Cerrar(sb);
    }

    public static string RecepcionAlmacen(int recepcionId, DateTime fecha, string almacen, string proveedor, string? nroFactura,
        string usuario, IEnumerable<LineaAviso> productos, int totalUnidades)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📦 Enviaron al {almacen} {totalUnidades} u — {Dia(fecha)}");
        sb.Append("Recepción N° ").Append(recepcionId).Append(" · Proveedor: ").Append(proveedor);
        if (!string.IsNullOrWhiteSpace(nroFactura)) sb.Append(" · Factura ").Append(nroFactura);
        sb.AppendLine();
        AgregarProductos(sb, productos);
        sb.AppendLine($"Registró: {usuario}");
        return Cerrar(sb);
    }

    // ---------- Ruta ----------

    public static string CargaVendedor(int salidaId, DateTime fecha, IEnumerable<LineaAviso> productos, int totalCarga, int enPoder)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🚚 Carga registrada — salida N° {salidaId} · {Fecha(fecha)}");
        AgregarProductos(sb, productos);
        sb.AppendLine($"Total de esta carga: {totalCarga} u · Tenés en tu poder: {enPoder} u");
        return Cerrar(sb);
    }

    public static string CargaAdmin(int salidaId, string vendedor, DateTime fecha, string usuario,
        IReadOnlyList<(string Almacen, int Unidades)> porAlmacen, IEnumerable<LineaAviso> productos, int totalCarga, int enPoder)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🚚 Carga a ruta — {vendedor} (salida N° {salidaId})");
        sb.AppendLine($"{Fecha(fecha)} · Registró: {usuario}");
        sb.AppendLine("Desde " + string.Join(" · ", porAlmacen.Select(a => $"{a.Almacen}: {a.Unidades} u")));
        AgregarProductos(sb, productos);
        sb.AppendLine($"Total de esta carga: {totalCarga} u · En poder de {vendedor}: {enPoder} u");
        return Cerrar(sb);
    }

    public static string CargaEncargado(int salidaId, string vendedor, string almacen, string usuario,
        IEnumerable<LineaAviso> productos, int unidadesDelAlmacen)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📤 Salieron {unidadesDelAlmacen} u del {almacen} — carga a ruta de {vendedor} (salida N° {salidaId})");
        AgregarProductos(sb, productos);
        sb.AppendLine($"Registró: {usuario}");
        return Cerrar(sb);
    }

    public static string DevolucionVendedor(int salidaId, DateTime fecha, IEnumerable<LineaAviso> productos, int totalDevuelto, int enPoder)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"↩️ Devolución registrada — salida N° {salidaId} · {Fecha(fecha)}");
        AgregarProductos(sb, productos);
        sb.AppendLine($"Total devuelto: {totalDevuelto} u · Te quedan en tu poder: {enPoder} u");
        return Cerrar(sb);
    }

    public static string DevolucionAdmin(int salidaId, string vendedor, DateTime fecha, string usuario,
        IReadOnlyList<(string Almacen, int Unidades)> porAlmacen, IEnumerable<LineaAviso> productos, int totalDevuelto, int enPoder)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"↩️ Devolución de ruta — {vendedor} (salida N° {salidaId})");
        sb.AppendLine($"{Fecha(fecha)} · Registró: {usuario}");
        sb.AppendLine("Devolvió a " + string.Join(" · ", porAlmacen.Select(a => $"{a.Almacen}: {a.Unidades} u")));
        AgregarProductos(sb, productos);
        sb.AppendLine($"Total devuelto: {totalDevuelto} u · En poder de {vendedor}: {enPoder} u");
        return Cerrar(sb);
    }

    public static string DevolucionEncargado(int salidaId, string vendedor, string almacen, string usuario,
        IEnumerable<LineaAviso> productos, int unidadesDelAlmacen)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📥 Entraron {unidadesDelAlmacen} u al {almacen} — devolución de ruta de {vendedor} (salida N° {salidaId})");
        AgregarProductos(sb, productos);
        sb.AppendLine($"Registró: {usuario}");
        return Cerrar(sb);
    }

    public static string CierreSalidaAdmin(int salidaId, string vendedor, int cargado, int devuelto, int vendido,
        int? nroVenta, decimal montoNeto, decimal descuento, decimal entregado, string? medio, decimal falta, string usuario)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"✅ Salida N° {salidaId} cerrada — {vendedor}");
        sb.AppendLine($"Cargado {cargado} u · Devuelto {devuelto} u · Vendido {vendido} u");
        if (nroVenta.HasValue)
        {
            sb.Append($"Venta de ruta N° {nroVenta}: {Bs(montoNeto)}");
            if (descuento > 0) sb.Append($" (desc. {Bs(descuento)})");
            sb.AppendLine();
            sb.Append($"Entregó: {Bs(entregado)}");
            if (entregado > 0 && !string.IsNullOrEmpty(medio)) sb.Append($" ({Medio(medio)})");
            sb.AppendLine($" · Debe: {Bs(falta)}");
        }
        else
            sb.AppendLine("Devolvió todo: se cerró sin venta");
        sb.AppendLine($"Cerró: {usuario}");
        return Cerrar(sb);
    }

    public static string CierreSalidaVendedor(int salidaId, int vendido, int? nroVenta, decimal montoNeto, decimal descuento,
        decimal entregado, string? medio, decimal falta)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🧾 Salida N° {salidaId} cerrada");
        if (nroVenta.HasValue)
        {
            sb.Append($"Vendiste {vendido} u por {Bs(montoNeto)}");
            if (descuento > 0) sb.Append($" (desc. {Bs(descuento)})");
            sb.AppendLine();
            sb.Append($"Entregaste: {Bs(entregado)}");
            if (entregado > 0 && !string.IsNullOrEmpty(medio)) sb.Append($" ({Medio(medio)})");
            sb.AppendLine(falta > 0 ? $" · *Te falta rendir: {Bs(falta)}*" : " · No te falta rendir nada");
        }
        else
            sb.AppendLine("Devolviste todo: la salida se cerró sin venta");
        return Cerrar(sb);
    }

    public static string AbonoVendedor(decimal monto, decimal descuento, string? medio, decimal saldo)
    {
        var sb = new StringBuilder();
        sb.Append("💵 Abono registrado: ").Append(Bs(monto));
        if (monto > 0 && !string.IsNullOrEmpty(medio)) sb.Append($" ({Medio(medio)})");
        if (descuento > 0) sb.Append($" · Descuento: {Bs(descuento)}");
        sb.AppendLine();
        sb.AppendLine(saldo > 0 ? $"Saldo que te falta rendir: {Bs(saldo)}" : "Saldo: Bs 0,00 · ¡No te falta rendir nada!");
        return Cerrar(sb);
    }

    // ---------- Anulaciones (S1) ----------

    public static string Anulacion(string tipo, string referencia, string? detalle, string usuario, DateTime fecha)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"⚠️ Anulación — {tipo}");
        sb.AppendLine(referencia);
        if (!string.IsNullOrWhiteSpace(detalle)) sb.AppendLine(detalle);
        sb.AppendLine($"Anuló: {usuario} · {Fecha(fecha)}");
        return Cerrar(sb);
    }
}
