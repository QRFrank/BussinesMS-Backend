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
/// Formato: encabezado, línea en blanco, un dato por línea ("Etiqueta: valor"), línea en blanco, productos, línea en blanco, firma.
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

    /// <summary>Texto del medio de pago: "efectivo" / "transferencia" o, si es mixto, "efectivo Bs 100,00 + transferencia Bs 50,00". Null si no hay pagos.</summary>
    public static string? TextoMedios(IReadOnlyList<(BussinesMS.Dominio.Enums.Navidad.MedioPagoNav Medio, decimal Monto)>? pagos)
    {
        if (pagos == null || pagos.Count == 0) return null;
        if (pagos.Count == 1) return pagos[0].Medio.ToString().ToLowerInvariant();
        return string.Join(" + ", pagos.OrderByDescending(p => p.Medio == BussinesMS.Dominio.Enums.Navidad.MedioPagoNav.Efectivo)
            .Select(p => $"{p.Medio.ToString().ToLowerInvariant()} {Bs(p.Monto)}"));
    }

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

    /// <summary>Bloque de productos: línea en blanco y luego "• Producto: cantidad" (máx. 15) y "• y N más". Si no hay productos, no agrega nada.</summary>
    public static void AgregarProductos(StringBuilder sb, IEnumerable<LineaAviso> lineas)
    {
        var lista = lineas.Where(l => l.Unidades != 0).OrderBy(l => l.Nombre).ToList();
        if (lista.Count == 0) return;
        sb.AppendLine();
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

    /// <summary>Línea en blanco + firma.</summary>
    private static string Cerrar(StringBuilder sb)
    {
        sb.AppendLine();
        sb.Append("— ").Append(Firma);
        return sb.ToString();
    }

    /// <summary>Encabezado seguido de una línea en blanco.</summary>
    private static void Encabezado(StringBuilder sb, string texto)
    {
        sb.AppendLine(texto);
        sb.AppendLine();
    }

    // ---------- Tienda y caja (decisión 8.8) ----------

    public static string TiendaAbiertaOCerrada(bool apertura, string almacen, DateTime fechaHora, string usuario,
        DateTime? diaPendiente, int productos, int totalContado, IReadOnlyList<DiferenciaConteoAviso> diferencias)
    {
        var sb = new StringBuilder();
        Encabezado(sb, apertura ? $"🏪 Tienda abierta — {almacen}" : $"🔒 Tienda cerrada — {almacen}");
        sb.AppendLine($"Fecha: {Fecha(fechaHora)}");
        sb.AppendLine($"{(apertura ? "Abrió" : "Cerró")}: {usuario}");
        if (diaPendiente.HasValue)
            sb.AppendLine($"Cierre atrasado del día: {Dia(diaPendiente.Value)}");
        sb.AppendLine($"Productos contados: {productos}");
        sb.AppendLine($"Total contado: {totalContado} u");
        if (diferencias.Count == 0)
            sb.AppendLine("Diferencias: ninguna");
        else
        {
            var sobran = diferencias.Where(d => d.Diferencia > 0).Sum(d => d.Diferencia);
            var faltan = -diferencias.Where(d => d.Diferencia < 0).Sum(d => d.Diferencia);
            sb.AppendLine($"⚠️ Diferencias: {diferencias.Count} producto(s)");
            sb.AppendLine($"Sobran: {sobran} u");
            sb.AppendLine($"Faltan: {faltan} u");
            sb.AppendLine();
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
        Encabezado(sb, diferencia != 0 ? $"⚠️ Cierre de caja CON DIFERENCIA — {almacen}" : $"💰 Cierre de caja — {almacen}");
        sb.AppendLine($"Turno: {Fecha(desde)} → {Fecha(hasta)}");
        sb.AppendLine($"Registró: {usuario}");
        sb.AppendLine($"Vendido: {Bs(totalVendido)}");
        sb.AppendLine($"Efectivo esperado: {Bs(efectivoEsperado)}");
        sb.AppendLine($"Entregado: {Bs(efectivoEntregado)}");
        if (diferencia == 0)
            sb.AppendLine("Diferencia: ninguna");
        else if (diferencia < 0)
            sb.AppendLine($"Diferencia: *FALTA {Bs(-diferencia)}*");
        else
            sb.AppendLine($"Diferencia: *SOBRA {Bs(diferencia)}*");
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
        Encabezado(sb, $"📦 Recepción N° {recepcionId} — {Dia(fecha)}");
        sb.AppendLine($"Proveedor: {proveedor}");
        if (!string.IsNullOrWhiteSpace(nroFactura)) sb.AppendLine($"Factura: {nroFactura}");
        foreach (var a in porAlmacen) sb.AppendLine($"{a.Almacen}: {a.Unidades} u");
        sb.AppendLine($"Total: {porAlmacen.Sum(a => a.Unidades)} u");
        sb.AppendLine($"Registró: {usuario}");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string RecepcionAlmacen(int recepcionId, DateTime fecha, string almacen, string proveedor, string? nroFactura,
        string usuario, IEnumerable<LineaAviso> productos, int totalUnidades)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"📦 Enviaron al {almacen} {totalUnidades} u — {Dia(fecha)}");
        sb.AppendLine($"Recepción N°: {recepcionId}");
        sb.AppendLine($"Proveedor: {proveedor}");
        if (!string.IsNullOrWhiteSpace(nroFactura)) sb.AppendLine($"Factura: {nroFactura}");
        sb.AppendLine($"Registró: {usuario}");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    // ---------- Compras ----------

    public static string CompraAdmin(int compraId, DateTime fecha, string proveedor, string? nroNota, string usuario,
        IReadOnlyList<(string Almacen, int Unidades)> porAlmacen, IEnumerable<LineaAviso> productos)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"🛒 Compra N° {compraId} — {Dia(fecha)}");
        sb.AppendLine($"Proveedor: {proveedor}");
        if (!string.IsNullOrWhiteSpace(nroNota)) sb.AppendLine($"Nota: {nroNota}");
        sb.AppendLine($"Registró: {usuario}");
        AgregarProductos(sb, productos);
        sb.AppendLine();
        foreach (var a in porAlmacen) sb.AppendLine($"{a.Almacen}: {a.Unidades} u");
        sb.AppendLine($"Total: {porAlmacen.Sum(a => a.Unidades)} u");
        return Cerrar(sb);
    }

    public static string CompraAlmacen(int compraId, DateTime fecha, string almacen, string proveedor, string? nroNota,
        string usuario, IEnumerable<LineaAviso> productos, int totalUnidades)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"🛒 Compra para el {almacen}: {totalUnidades} u — {Dia(fecha)}");
        sb.AppendLine($"Compra N°: {compraId}");
        sb.AppendLine($"Proveedor: {proveedor}");
        if (!string.IsNullOrWhiteSpace(nroNota)) sb.AppendLine($"Nota: {nroNota}");
        sb.AppendLine($"Registró: {usuario}");
        AgregarProductos(sb, productos);
        sb.AppendLine();
        sb.AppendLine($"Total para tu almacén: {totalUnidades} u");
        return Cerrar(sb);
    }

    // ---------- Ventas y reservas de tienda (a los encargados de la tienda; sin productos) ----------

    public static string VentaTienda(bool reserva, int nroVenta, string tienda, DateTime fecha, string vendedor,
        decimal montoNeto, decimal descuento, bool credito, decimal pagado, string? medios, decimal saldo)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"{(reserva ? "📝 Reserva" : "🛍️ Venta")} N° {nroVenta} — {tienda}");
        sb.AppendLine($"Fecha: {Fecha(fecha)}");
        sb.AppendLine($"Vendedor: {vendedor}");
        sb.Append($"Total: {Bs(montoNeto)}");
        if (descuento > 0) sb.Append($" (con descuento de {Bs(descuento)})");
        sb.AppendLine();
        sb.AppendLine($"Condición: {(credito ? "Crédito" : "Contado")}");
        sb.Append($"Pagado: {Bs(pagado)}");
        if (pagado > 0 && !string.IsNullOrEmpty(medios)) sb.Append($" ({medios})");
        sb.AppendLine();
        if (credito || saldo > 0) sb.AppendLine($"Saldo: {Bs(saldo)}");
        return Cerrar(sb);
    }

    // ---------- Consumos ----------

    public static string ConsumoAdmin(int consumoId, DateTime fecha, string almacen, string usuario, string motivo,
        string? observacion, IEnumerable<LineaAviso> productos, int totalUnidades)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"🍽️ Consumo N° {consumoId} — {Dia(fecha)}");
        sb.AppendLine($"Almacén: {almacen}");
        sb.AppendLine($"Registró: {usuario}");
        sb.AppendLine($"Motivo: {motivo}");
        if (!string.IsNullOrWhiteSpace(observacion)) sb.AppendLine($"Descripción: {observacion}");
        sb.AppendLine($"Total: {totalUnidades} u");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string ConsumoAlmacen(int consumoId, DateTime fecha, string almacen, string usuario, string motivo,
        string? observacion, IEnumerable<LineaAviso> productos, int totalUnidades)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"🍽️ Consumo en el {almacen}: {totalUnidades} u — {Dia(fecha)}");
        sb.AppendLine($"Consumo N°: {consumoId}");
        sb.AppendLine($"Registró: {usuario}");
        sb.AppendLine($"Motivo: {motivo}");
        if (!string.IsNullOrWhiteSpace(observacion)) sb.AppendLine($"Descripción: {observacion}");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    // ---------- Ruta ----------

    public static string CargaVendedor(int salidaId, DateTime fecha, IEnumerable<LineaAviso> productos, int totalCarga, int enPoder)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"🚚 Carga registrada — salida N° {salidaId}");
        sb.AppendLine($"Fecha: {Fecha(fecha)}");
        sb.AppendLine($"Total de esta carga: {totalCarga} u");
        sb.AppendLine($"Tenés en tu poder: {enPoder} u");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string CargaAdmin(int salidaId, string vendedor, DateTime fecha, string usuario,
        IReadOnlyList<(string Almacen, int Unidades)> porAlmacen, IEnumerable<LineaAviso> productos, int totalCarga, int enPoder)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"🚚 Carga a ruta — {vendedor} (salida N° {salidaId})");
        sb.AppendLine($"Fecha: {Fecha(fecha)}");
        sb.AppendLine($"Registró: {usuario}");
        foreach (var a in porAlmacen) sb.AppendLine($"Desde {a.Almacen}: {a.Unidades} u");
        sb.AppendLine($"Total de esta carga: {totalCarga} u");
        sb.AppendLine($"En poder de {vendedor}: {enPoder} u");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string CargaEncargado(int salidaId, string vendedor, string almacen, string usuario,
        IEnumerable<LineaAviso> productos, int unidadesDelAlmacen)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"📤 Salieron {unidadesDelAlmacen} u del {almacen} — carga a ruta de {vendedor} (salida N° {salidaId})");
        sb.AppendLine($"Registró: {usuario}");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string DevolucionVendedor(int salidaId, DateTime fecha, IEnumerable<LineaAviso> productos, int totalDevuelto, int enPoder)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"↩️ Devolución registrada — salida N° {salidaId}");
        sb.AppendLine($"Fecha: {Fecha(fecha)}");
        sb.AppendLine($"Total devuelto: {totalDevuelto} u");
        sb.AppendLine($"Te quedan en tu poder: {enPoder} u");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string DevolucionAdmin(int salidaId, string vendedor, DateTime fecha, string usuario,
        IReadOnlyList<(string Almacen, int Unidades)> porAlmacen, IEnumerable<LineaAviso> productos, int totalDevuelto, int enPoder)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"↩️ Devolución de ruta — {vendedor} (salida N° {salidaId})");
        sb.AppendLine($"Fecha: {Fecha(fecha)}");
        sb.AppendLine($"Registró: {usuario}");
        foreach (var a in porAlmacen) sb.AppendLine($"Devolvió a {a.Almacen}: {a.Unidades} u");
        sb.AppendLine($"Total devuelto: {totalDevuelto} u");
        sb.AppendLine($"En poder de {vendedor}: {enPoder} u");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string DevolucionEncargado(int salidaId, string vendedor, string almacen, string usuario,
        IEnumerable<LineaAviso> productos, int unidadesDelAlmacen)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"📥 Entraron {unidadesDelAlmacen} u al {almacen} — devolución de ruta de {vendedor} (salida N° {salidaId})");
        sb.AppendLine($"Registró: {usuario}");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string CierreSalidaAdmin(int salidaId, string vendedor, int cargado, int devuelto, int vendido,
        int? nroVenta, decimal montoNeto, decimal descuento, decimal entregado, string? medio, decimal falta, string usuario)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"✅ Salida N° {salidaId} cerrada — {vendedor}");
        sb.AppendLine($"Cargado: {cargado} u");
        sb.AppendLine($"Devuelto: {devuelto} u");
        sb.AppendLine($"Vendido: {vendido} u");
        if (nroVenta.HasValue)
        {
            sb.AppendLine($"Venta de ruta N°: {nroVenta}");
            sb.AppendLine($"Monto: {Bs(montoNeto)}");
            if (descuento > 0) sb.AppendLine($"Descuento: {Bs(descuento)}");
            sb.Append($"Entregó: {Bs(entregado)}");
            if (entregado > 0 && !string.IsNullOrEmpty(medio)) sb.Append($" ({medio})");
            sb.AppendLine();
            sb.AppendLine($"Debe: {Bs(falta)}");
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
        Encabezado(sb, $"🧾 Salida N° {salidaId} cerrada");
        if (nroVenta.HasValue)
        {
            sb.AppendLine($"Vendiste: {vendido} u");
            sb.AppendLine($"Monto: {Bs(montoNeto)}");
            if (descuento > 0) sb.AppendLine($"Descuento: {Bs(descuento)}");
            sb.Append($"Entregaste: {Bs(entregado)}");
            if (entregado > 0 && !string.IsNullOrEmpty(medio)) sb.Append($" ({medio})");
            sb.AppendLine();
            sb.AppendLine(falta > 0 ? $"*Te falta rendir: {Bs(falta)}*" : "No te falta rendir nada");
        }
        else
            sb.AppendLine("Devolviste todo: la salida se cerró sin venta");
        return Cerrar(sb);
    }

    public static string AbonoVendedor(decimal monto, decimal descuento, string? medio, decimal saldo)
    {
        var sb = new StringBuilder();
        Encabezado(sb, "💵 Abono registrado");
        sb.Append("Monto: ").Append(Bs(monto));
        if (monto > 0 && !string.IsNullOrEmpty(medio)) sb.Append($" ({medio})");
        sb.AppendLine();
        if (descuento > 0) sb.AppendLine($"Descuento: {Bs(descuento)}");
        sb.AppendLine(saldo > 0 ? $"Saldo que te falta rendir: {Bs(saldo)}" : "Saldo: Bs 0,00 (¡No te falta rendir nada!)");
        return Cerrar(sb);
    }

    // ---------- Traslados ----------

    public static string Traslado(int trasladoId, DateTime fecha, string origen, string destino, string usuario,
        string? motivo, IEnumerable<LineaAviso> productos, int totalUnidades)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"🔁 Traslado N° {trasladoId} — {Dia(fecha)}");
        sb.AppendLine($"Origen: {origen}");
        sb.AppendLine($"Destino: {destino}");
        sb.AppendLine($"Registró: {usuario}");
        sb.AppendLine($"Total: {totalUnidades} u");
        if (!string.IsNullOrWhiteSpace(motivo)) sb.AppendLine($"Motivo: {motivo}");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string TrasladoSalidaAlmacen(int trasladoId, DateTime fecha, string origen, string destino, string usuario,
        string? motivo, IEnumerable<LineaAviso> productos, int totalUnidades)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"📤 Salida por traslado — {origen}");
        sb.AppendLine($"Traslado N°: {trasladoId} — {Dia(fecha)}");
        sb.AppendLine($"Hacia: {destino}");
        sb.AppendLine($"Registró: {usuario}");
        if (!string.IsNullOrWhiteSpace(motivo)) sb.AppendLine($"Motivo: {motivo}");
        sb.AppendLine($"Total: {totalUnidades} u");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    public static string TrasladoIngresoAlmacen(int trasladoId, DateTime fecha, string origen, string destino, string usuario,
        string? motivo, IEnumerable<LineaAviso> productos, int totalUnidades)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"📥 Ingreso por traslado — {destino}");
        sb.AppendLine($"Traslado N°: {trasladoId} — {Dia(fecha)}");
        sb.AppendLine($"Desde: {origen}");
        sb.AppendLine($"Registró: {usuario}");
        if (!string.IsNullOrWhiteSpace(motivo)) sb.AppendLine($"Motivo: {motivo}");
        sb.AppendLine($"Total: {totalUnidades} u");
        AgregarProductos(sb, productos);
        return Cerrar(sb);
    }

    // ---------- Anulaciones (S1) ----------

    public static string Anulacion(string tipo, string referencia, string? detalle, string usuario, DateTime fecha)
    {
        var sb = new StringBuilder();
        Encabezado(sb, $"⚠️ Anulación — {tipo}");
        sb.AppendLine(referencia);
        if (!string.IsNullOrWhiteSpace(detalle)) sb.AppendLine(detalle);
        sb.AppendLine($"Anuló: {usuario}");
        sb.AppendLine($"Fecha: {Fecha(fecha)}");
        return Cerrar(sb);
    }

    // ---------- Comprobante de venta al cliente (C1): pie del PDF ----------

    public const string AdjuntoComprobanteVentaPdf = "ComprobanteVentaPdf";
    // C7: comprobante de una venta de "Mis ventas" del vendedor de ruta (ReferenciaId = VentaClienteRuta.Id)
    public const string AdjuntoComprobanteMisVentasPdf = "ComprobanteMisVentasPdf";

    // Comprobante de una entrega de reserva (ReferenciaId = EntregaVenta.Id): sin precios, saldo ni pendientes
    public const string AdjuntoComprobanteEntregaPdf = "ComprobanteEntregaPdf";

    public static string CaptionComprobanteEntrega(int nroVenta, int productos, int unidades)
        => $"📦 Comprobante de entrega — Reserva N° {nroVenta}\n"
           + $"{productos} {(productos == 1 ? "producto" : "productos")} — {unidades} u\n— {Firma}";

    public static string CaptionComprobante(int nroVenta, decimal total)
        => $"🧾 Comprobante de venta N° {nroVenta}\nTotal: {Bs(total)}\n— {Firma}";

    // Reservas: además del total, lo pagado y el saldo que falta pagar
    public static string CaptionComprobanteReserva(int nroVenta, decimal total, decimal pagado, decimal saldo)
        => $"🧾 Comprobante de reserva N° {nroVenta}\nTotal: {Bs(total)}\nPagado: {Bs(pagado)}\n"
           + (saldo > 0 ? $"Saldo por pagar: {Bs(saldo)}" : "Pagado completo")
           + $"\n— {Firma}";
}
