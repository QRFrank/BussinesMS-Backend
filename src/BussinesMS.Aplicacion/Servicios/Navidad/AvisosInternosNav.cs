using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductoNav = BussinesMS.Dominio.Entidades.Navidad.Producto;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Avisos internos inmediatos de WhatsApp (fase 3). Solo arma datos + texto (MensajesAvisosNav) y encola con INotificadorNav.
// Se llama dentro de la transaccion de cada operacion; nunca lanza (WhatsApp no puede romper una operacion de negocio).
// Eventos: se reutilizan los de EventoNotificacionNav (el mismo evento cubre a todos los destinatarios de esa operacion).
public class AvisosInternosNav : IAvisosInternosNav
{
    private readonly INotificadorNav _notificador;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IVendedorNavRepository _vendedorRepo;
    private readonly IVentaNavRepository _ventaRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AvisosInternosNav> _logger;

    public AvisosInternosNav(
        INotificadorNav notificador,
        IAlmacenRepository almacenRepo,
        IUsuarioRepository usuarioRepo,
        IProductoNavRepository productoRepo,
        IVendedorNavRepository vendedorRepo,
        IVentaNavRepository ventaRepo,
        ICurrentUserService currentUser,
        ILogger<AvisosInternosNav> logger)
    {
        _notificador = notificador;
        _almacenRepo = almacenRepo;
        _usuarioRepo = usuarioRepo;
        _productoRepo = productoRepo;
        _vendedorRepo = vendedorRepo;
        _ventaRepo = ventaRepo;
        _currentUser = currentUser;
        _logger = logger;
    }

    // ================= Tienda y caja =================

    public Task TiendaAsync(ConteoInventario conteo) => SeguroAsync("tienda", async () =>
    {
        var apertura = conteo.Tipo == TipoConteoInventario.Apertura;
        var autorId = _currentUser.GetUsuarioId();
        var almacen = (await NombresAlmacenAsync(new[] { conteo.AlmacenId })).GetValueOrDefault(conteo.AlmacenId, $"#{conteo.AlmacenId}");
        var usuario = await NombreUsuarioAsync(conteo.UsuarioId);
        var productos = await ProductosAsync(conteo.Detalles.Select(d => d.ProductoId));
        var difs = conteo.Detalles.Where(d => d.Diferencia != 0)
            .Select(d => new DiferenciaConteoAviso(NombreProducto(productos, d.ProductoId), d.StockSistema, d.Contado, d.Diferencia))
            .ToList();
        var hoy = BoliviaTimeZone.Now();
        var texto = MensajesAvisosNav.TiendaAbiertaOCerrada(apertura, almacen, hoy, usuario,
            conteo.Fecha.Date < hoy.Date ? conteo.Fecha : null,
            conteo.Detalles.Count, conteo.Detalles.Sum(d => d.Contado), difs);
        var evento = apertura ? EventoNotificacionNav.AperturaCaja : EventoNotificacionNav.CierreTienda;
        var clave = $"{(apertura ? "tienda-apertura" : "tienda-cierre")}:{conteo.Id}";
        await EncolarAdminsYVendedorAsync(conteo.TemporadaId, evento, texto, clave, autorId);
    });

    public Task CierreCajaAsync(CierreCajaTienda caja) => SeguroAsync("cierre de caja", async () =>
    {
        var autorId = _currentUser.GetUsuarioId();
        var almacen = (await NombresAlmacenAsync(new[] { caja.AlmacenId })).GetValueOrDefault(caja.AlmacenId, $"#{caja.AlmacenId}");
        var usuario = await NombreUsuarioAsync(caja.UsuarioId);
        var texto = MensajesAvisosNav.CierreCaja(almacen, BoliviaTimeZone.ToLocal(caja.Desde), BoliviaTimeZone.ToLocal(caja.Hasta),
            usuario, caja.TotalVendido, caja.EfectivoEsperado, caja.EfectivoEntregado, caja.Diferencia,
            caja.Transferencias, caja.Credito, caja.Observacion);
        await EncolarAdminsYVendedorAsync(caja.TemporadaId, EventoNotificacionNav.CierreCaja, texto, $"cierre-caja:{caja.Id}", autorId);
    });

    // Admins (sin el autor) + el vendedor que lo hizo (le llega aunque sea el autor: es su comprobante)
    private async Task EncolarAdminsYVendedorAsync(int temporadaId, EventoNotificacionNav evento, string texto, string clave, int? autorId)
    {
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = temporadaId,
            Evento = evento,
            Prioridad = 2,
            Mensaje = texto,
            Audiencia = AudienciaNotificacionNav.Admins,
            ExcluirUsuarioId = autorId,
            ClaveUnica = $"{clave}:admins"
        });

        if (!autorId.HasValue) return;
        var vendedorId = await _vendedorRepo.AsQueryable().AsNoTracking()
            .Where(v => v.IsActive && v.TemporadaId == temporadaId && v.UsuarioId == autorId.Value)
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync();
        if (!vendedorId.HasValue) return;
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = temporadaId,
            Evento = evento,
            Prioridad = 2,
            Mensaje = texto,
            Audiencia = AudienciaNotificacionNav.Vendedor,
            AudienciaId = vendedorId.Value,
            ClaveUnica = $"{clave}:vendedor"
        });
    }

    // ================= Recepciones =================

    public Task RecepcionAsync(Recepcion recepcion, string proveedorNombre) => SeguroAsync("recepcion", async () =>
    {
        var autorId = _currentUser.GetUsuarioId();
        var dists = recepcion.Detalles
            .SelectMany(d => d.Distribuciones.Select(x => (x.AlmacenId, d.ProductoId, Unidades: x.CantidadUnidades)))
            .Where(x => x.Unidades > 0)
            .ToList();
        if (dists.Count == 0) return;

        var almacenes = await NombresAlmacenAsync(dists.Select(x => x.AlmacenId));
        var productos = await ProductosAsync(dists.Select(x => x.ProductoId));
        var usuario = autorId.HasValue ? await NombreUsuarioAsync(autorId.Value) : "-";
        var fecha = recepcion.Fecha;

        // A3: admins
        var porAlmacen = dists.GroupBy(x => x.AlmacenId)
            .Select(g => (Almacen: almacenes.GetValueOrDefault(g.Key, $"#{g.Key}"), Unidades: g.Sum(x => x.Unidades)))
            .OrderBy(x => x.Almacen).ToList();
        var lineasTotal = dists.GroupBy(x => x.ProductoId).Select(g => Linea(productos, g.Key, g.Sum(x => x.Unidades))).ToList();
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = recepcion.TemporadaId,
            Evento = EventoNotificacionNav.RecepcionAdmin,
            Mensaje = MensajesAvisosNav.RecepcionAdmin(recepcion.Id, fecha, proveedorNombre, recepcion.NroFactura, usuario, porAlmacen, lineasTotal),
            Audiencia = AudienciaNotificacionNav.Admins,
            ExcluirUsuarioId = autorId,
            ClaveUnica = $"recepcion:{recepcion.Id}:admins"
        });

        // E1: un aviso por almacen de destino
        foreach (var g in dists.GroupBy(x => x.AlmacenId))
        {
            var lineas = g.GroupBy(x => x.ProductoId).Select(p => Linea(productos, p.Key, p.Sum(x => x.Unidades))).ToList();
            await _notificador.EncolarAsync(new NuevaNotificacionNav
            {
                TemporadaId = recepcion.TemporadaId,
                Evento = EventoNotificacionNav.RecepcionAlmacen,
                Mensaje = MensajesAvisosNav.RecepcionAlmacen(recepcion.Id, fecha, almacenes.GetValueOrDefault(g.Key, $"#{g.Key}"),
                    proveedorNombre, recepcion.NroFactura, usuario, lineas, g.Sum(x => x.Unidades)),
                Audiencia = AudienciaNotificacionNav.EncargadosAlmacen,
                AudienciaId = g.Key,
                ExcluirUsuarioId = autorId,
                ClaveUnica = $"recepcion:{recepcion.Id}:almacen:{g.Key}"
            });
        }
    });

    // ================= Ruta =================

    public Task CargaRutaAsync(SalidaRuta salida, SalidaRutaCarga carga) => SeguroAsync("carga de ruta", async () =>
    {
        var autorId = _currentUser.GetUsuarioId();
        var detalles = carga.Detalles.Where(d => d.Unidades > 0)
            .Select(d => (AlmacenId: d.AlmacenOrigenId, d.ProductoId, d.Unidades)).ToList();
        if (detalles.Count == 0) return;

        var almacenes = await NombresAlmacenAsync(detalles.Select(x => x.AlmacenId));
        var productos = await ProductosAsync(detalles.Select(x => x.ProductoId));
        var vendedor = await NombreVendedorAsync(salida.VendedorId);
        var usuario = autorId.HasValue ? await NombreUsuarioAsync(autorId.Value) : "-";
        var fecha = BoliviaTimeZone.Now();
        var totalCarga = detalles.Sum(x => x.Unidades);
        var enPoder = EnPoder(salida);
        var lineasTotal = detalles.GroupBy(x => x.ProductoId).Select(g => Linea(productos, g.Key, g.Sum(x => x.Unidades))).ToList();
        var porAlmacen = PorAlmacen(detalles, almacenes);
        var cuando = $"carga:{carga.Id}";

        // V1: al vendedor (le llega aunque el la haya registrado)
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = salida.TemporadaId,
            Evento = EventoNotificacionNav.CargaRuta,
            Mensaje = MensajesAvisosNav.CargaVendedor(salida.Id, fecha, lineasTotal, totalCarga, enPoder),
            Audiencia = AudienciaNotificacionNav.Vendedor,
            AudienciaId = salida.VendedorId,
            ClaveUnica = $"{cuando}:vendedor"
        });
        // A4: admins
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = salida.TemporadaId,
            Evento = EventoNotificacionNav.CargaRuta,
            Mensaje = MensajesAvisosNav.CargaAdmin(salida.Id, vendedor, fecha, usuario, porAlmacen, lineasTotal, totalCarga, enPoder),
            Audiencia = AudienciaNotificacionNav.Admins,
            ExcluirUsuarioId = autorId,
            ClaveUnica = $"{cuando}:admins"
        });
        // E2: encargados de cada almacen de origen
        foreach (var g in detalles.GroupBy(x => x.AlmacenId))
        {
            var lineas = g.GroupBy(x => x.ProductoId).Select(p => Linea(productos, p.Key, p.Sum(x => x.Unidades))).ToList();
            await _notificador.EncolarAsync(new NuevaNotificacionNav
            {
                TemporadaId = salida.TemporadaId,
                Evento = EventoNotificacionNav.CargaRuta,
                Mensaje = MensajesAvisosNav.CargaEncargado(salida.Id, vendedor, almacenes.GetValueOrDefault(g.Key, $"#{g.Key}"),
                    usuario, lineas, g.Sum(x => x.Unidades)),
                Audiencia = AudienciaNotificacionNav.EncargadosAlmacen,
                AudienciaId = g.Key,
                ExcluirUsuarioId = autorId,
                ClaveUnica = $"{cuando}:almacen:{g.Key}"
            });
        }
    });

    public Task DevolucionRutaAsync(SalidaRuta salida, DevolucionRuta devolucion) => SeguroAsync("devolucion de ruta", async () =>
    {
        var autorId = _currentUser.GetUsuarioId();
        var detalles = devolucion.Detalles.Where(d => d.Unidades > 0)
            .Select(d => (AlmacenId: d.AlmacenDestinoId, d.ProductoId, d.Unidades)).ToList();
        if (detalles.Count == 0) return;

        var almacenes = await NombresAlmacenAsync(detalles.Select(x => x.AlmacenId));
        var productos = await ProductosAsync(detalles.Select(x => x.ProductoId));
        var vendedor = await NombreVendedorAsync(salida.VendedorId);
        var usuario = autorId.HasValue ? await NombreUsuarioAsync(autorId.Value) : "-";
        var fecha = BoliviaTimeZone.Now();
        var totalDevuelto = detalles.Sum(x => x.Unidades);
        var enPoder = EnPoder(salida);
        var lineasTotal = detalles.GroupBy(x => x.ProductoId).Select(g => Linea(productos, g.Key, g.Sum(x => x.Unidades))).ToList();
        var porAlmacen = PorAlmacen(detalles, almacenes);
        var cuando = $"devolucion:{devolucion.Id}";

        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = salida.TemporadaId,
            Evento = EventoNotificacionNav.DevolucionRuta,
            Mensaje = MensajesAvisosNav.DevolucionVendedor(salida.Id, fecha, lineasTotal, totalDevuelto, enPoder),
            Audiencia = AudienciaNotificacionNav.Vendedor,
            AudienciaId = salida.VendedorId,
            ClaveUnica = $"{cuando}:vendedor"
        });
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = salida.TemporadaId,
            Evento = EventoNotificacionNav.DevolucionRuta,
            Mensaje = MensajesAvisosNav.DevolucionAdmin(salida.Id, vendedor, fecha, usuario, porAlmacen, lineasTotal, totalDevuelto, enPoder),
            Audiencia = AudienciaNotificacionNav.Admins,
            ExcluirUsuarioId = autorId,
            ClaveUnica = $"{cuando}:admins"
        });
        foreach (var g in detalles.GroupBy(x => x.AlmacenId))
        {
            var lineas = g.GroupBy(x => x.ProductoId).Select(p => Linea(productos, p.Key, p.Sum(x => x.Unidades))).ToList();
            await _notificador.EncolarAsync(new NuevaNotificacionNav
            {
                TemporadaId = salida.TemporadaId,
                Evento = EventoNotificacionNav.DevolucionRuta,
                Mensaje = MensajesAvisosNav.DevolucionEncargado(salida.Id, vendedor, almacenes.GetValueOrDefault(g.Key, $"#{g.Key}"),
                    usuario, lineas, g.Sum(x => x.Unidades)),
                Audiencia = AudienciaNotificacionNav.EncargadosAlmacen,
                AudienciaId = g.Key,
                ExcluirUsuarioId = autorId,
                ClaveUnica = $"{cuando}:almacen:{g.Key}"
            });
        }
    });

    public Task CierreSalidaAsync(SalidaRuta salida, VentaNav? venta, decimal dineroEntregado, MedioPagoNav? medio)
        => SeguroAsync("cierre de salida", async () =>
        {
            var autorId = _currentUser.GetUsuarioId();
            var vendedor = await NombreVendedorAsync(salida.VendedorId);
            var usuario = autorId.HasValue ? await NombreUsuarioAsync(autorId.Value) : "-";
            var cargado = salida.Cargas.Where(c => !c.Anulada).SelectMany(c => c.Detalles).Sum(d => d.Unidades);
            var devuelto = salida.Devoluciones.Where(d => !d.Anulada).SelectMany(d => d.Detalles).Sum(d => d.Unidades);
            var vendido = Math.Max(0, cargado - devuelto);
            var medioTxt = dineroEntregado > 0 ? medio?.ToString() : null;
            int? nro = venta?.NroVenta;
            var neto = venta?.MontoNeto ?? 0;
            var desc = venta?.Descuento ?? 0;
            var falta = venta?.MontoSaldo ?? 0;

            await _notificador.EncolarAsync(new NuevaNotificacionNav
            {
                TemporadaId = salida.TemporadaId,
                Evento = EventoNotificacionNav.CierreSalidaRuta,
                Mensaje = MensajesAvisosNav.CierreSalidaAdmin(salida.Id, vendedor, cargado, devuelto, vendido, nro, neto, desc,
                    dineroEntregado, medioTxt, falta, usuario),
                Audiencia = AudienciaNotificacionNav.Admins,
                ExcluirUsuarioId = autorId,
                ClaveUnica = $"cierre-salida:{salida.Id}:admins"
            });
            await _notificador.EncolarAsync(new NuevaNotificacionNav
            {
                TemporadaId = salida.TemporadaId,
                Evento = EventoNotificacionNav.CierreSalidaRuta,
                Mensaje = MensajesAvisosNav.CierreSalidaVendedor(salida.Id, vendido, nro, neto, desc, dineroEntregado, medioTxt, falta),
                Audiencia = AudienciaNotificacionNav.Vendedor,
                AudienciaId = salida.VendedorId,
                ClaveUnica = $"cierre-salida:{salida.Id}:vendedor"
            });
        });

    public Task AbonoVendedorAsync(AbonoVendedor abono) => SeguroAsync("abono de vendedor", async () =>
    {
        // Saldo que le queda al vendedor en todas sus ventas de ruta (los cambios del abono ya estan guardados en la transaccion)
        var saldo = await _ventaRepo.AsQueryable().AsNoTracking()
            .Where(v => v.IsActive && !v.Anulada && v.Canal == CanalVentaNav.Ruta && v.TemporadaId == abono.TemporadaId && v.VendedorId == abono.VendedorId)
            .SumAsync(v => (decimal?)v.MontoSaldo) ?? 0;
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = abono.TemporadaId,
            Evento = EventoNotificacionNav.AbonoVendedor,
            Mensaje = MensajesAvisosNav.AbonoVendedor(abono.Monto, abono.Descuento, abono.Medio?.ToString(), saldo),
            Audiencia = AudienciaNotificacionNav.Vendedor,
            AudienciaId = abono.VendedorId,
            ClaveUnica = $"abono:{abono.Id}:vendedor"
        });
    });

    // ================= Traslados =================

    public Task TrasladoAsync(Traslado traslado) => SeguroAsync("traslado", async () =>
    {
        var autorId = _currentUser.GetUsuarioId();
        var almacenes = await NombresAlmacenAsync(new[] { traslado.AlmacenOrigenId, traslado.AlmacenDestinoId });
        var productos = await ProductosAsync(traslado.Detalles.Select(d => d.ProductoId));
        var usuario = await NombreUsuarioAsync(traslado.UsuarioId);
        var lineas = traslado.Detalles.GroupBy(d => d.ProductoId)
            .Select(g => Linea(productos, g.Key, g.Sum(x => x.CantidadUnidades))).ToList();
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = traslado.TemporadaId,
            Evento = EventoNotificacionNav.Traslado,
            Mensaje = MensajesAvisosNav.Traslado(traslado.Id, traslado.Fecha,
                almacenes.GetValueOrDefault(traslado.AlmacenOrigenId, $"#{traslado.AlmacenOrigenId}"),
                almacenes.GetValueOrDefault(traslado.AlmacenDestinoId, $"#{traslado.AlmacenDestinoId}"),
                usuario, traslado.Motivo, lineas, lineas.Sum(l => l.Unidades)),
            Audiencia = AudienciaNotificacionNav.Admins,
            ExcluirUsuarioId = autorId,
            ClaveUnica = $"traslado:{traslado.Id}:admins"
        });

        var origenNombre = almacenes.GetValueOrDefault(traslado.AlmacenOrigenId, $"#{traslado.AlmacenOrigenId}");
        var destinoNombre = almacenes.GetValueOrDefault(traslado.AlmacenDestinoId, $"#{traslado.AlmacenDestinoId}");
        var total = lineas.Sum(l => l.Unidades);
        // Encargados del almacen de origen (sale mercaderia)
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = traslado.TemporadaId,
            Evento = EventoNotificacionNav.TrasladoSalidaAlmacen,
            Mensaje = MensajesAvisosNav.TrasladoSalidaAlmacen(traslado.Id, traslado.Fecha, origenNombre, destinoNombre,
                usuario, traslado.Motivo, lineas, total),
            Audiencia = AudienciaNotificacionNav.EncargadosAlmacen,
            AudienciaId = traslado.AlmacenOrigenId,
            ExcluirUsuarioId = autorId,
            ClaveUnica = $"traslado:{traslado.Id}:origen:{traslado.AlmacenOrigenId}"
        });
        // Encargados del almacen de destino (entra mercaderia)
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = traslado.TemporadaId,
            Evento = EventoNotificacionNav.TrasladoIngresoAlmacen,
            Mensaje = MensajesAvisosNav.TrasladoIngresoAlmacen(traslado.Id, traslado.Fecha, origenNombre, destinoNombre,
                usuario, traslado.Motivo, lineas, total),
            Audiencia = AudienciaNotificacionNav.EncargadosAlmacen,
            AudienciaId = traslado.AlmacenDestinoId,
            ExcluirUsuarioId = autorId,
            ClaveUnica = $"traslado:{traslado.Id}:destino:{traslado.AlmacenDestinoId}"
        });
    });

    // ================= Anulaciones =================

    public Task AnulacionAsync(AnulacionAviso a) => SeguroAsync("anulacion", async () =>
    {
        var autorId = _currentUser.GetUsuarioId();
        var usuario = autorId.HasValue ? await NombreUsuarioAsync(autorId.Value) : "-";
        var detalle = a.Detalle;
        if (a.VendedorId.HasValue)
        {
            var vendedor = await NombreVendedorAsync(a.VendedorId.Value);
            detalle = string.IsNullOrWhiteSpace(detalle) ? $"Vendedor: {vendedor}" : $"Vendedor: {vendedor} · {detalle}";
        }
        await _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = a.TemporadaId,
            Evento = EventoNotificacionNav.Anulacion,
            Mensaje = MensajesAvisosNav.Anulacion(a.Tipo, a.Referencia, detalle, usuario, BoliviaTimeZone.Now()),
            Audiencia = AudienciaNotificacionNav.Admins,
            ExcluirUsuarioId = autorId,
            ClaveUnica = $"anulacion:{a.Clave}"
        });

        // Encargados de los almacenes involucrados (si la anulacion los informa), mismo texto, sin el autor
        foreach (var almacenId in (a.AlmacenesEncargadosIds ?? Array.Empty<int>()).Distinct())
        {
            await _notificador.EncolarAsync(new NuevaNotificacionNav
            {
                TemporadaId = a.TemporadaId,
                Evento = EventoNotificacionNav.Anulacion,
                Mensaje = MensajesAvisosNav.Anulacion(a.Tipo, a.Referencia, detalle, usuario, BoliviaTimeZone.Now()),
                Audiencia = AudienciaNotificacionNav.EncargadosAlmacen,
                AudienciaId = almacenId,
                ExcluirUsuarioId = autorId,
                ClaveUnica = $"anulacion:{a.Clave}:almacen:{almacenId}"
            });
        }
    });

    // ================= Helpers =================

    // WhatsApp nunca rompe una operacion de negocio
    private async Task SeguroAsync(string que, Func<Task> accion)
    {
        try
        {
            await accion();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo encolar el aviso de WhatsApp ({Que}); la operacion sigue", que);
        }
    }

    private static int EnPoder(SalidaRuta salida)
        => salida.Cargas.Where(c => !c.Anulada).SelectMany(c => c.Detalles).Sum(d => d.Unidades)
           - salida.Devoluciones.Where(d => !d.Anulada).SelectMany(d => d.Detalles).Sum(d => d.Unidades);

    private static List<(string Almacen, int Unidades)> PorAlmacen(
        IEnumerable<(int AlmacenId, int ProductoId, int Unidades)> detalles, Dictionary<int, string> almacenes)
        => detalles.GroupBy(x => x.AlmacenId)
            .Select(g => (Almacen: almacenes.GetValueOrDefault(g.Key, $"#{g.Key}"), Unidades: g.Sum(x => x.Unidades)))
            .OrderBy(x => x.Almacen).ToList();

    private async Task<Dictionary<int, string>> NombresAlmacenAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<int, string>();
        return await _almacenRepo.AsQueryable().AsNoTracking()
            .Where(a => lista.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Nombre);
    }

    private async Task<string> NombreUsuarioAsync(int usuarioId)
    {
        var u = await _usuarioRepo.AsQueryable().AsNoTracking()
            .Where(x => x.Id == usuarioId)
            .Select(x => new { x.Nombre, x.Apellido })
            .FirstOrDefaultAsync();
        if (u == null) return $"#{usuarioId}";
        var nombre = $"{u.Nombre?.Trim()} {u.Apellido?.Trim()}".Trim();
        return nombre.Length > 0 ? nombre : $"#{usuarioId}";
    }

    private async Task<string> NombreVendedorAsync(int vendedorId)
    {
        var usuarioId = await _vendedorRepo.AsQueryable().AsNoTracking()
            .Where(v => v.Id == vendedorId)
            .Select(v => (int?)v.UsuarioId)
            .FirstOrDefaultAsync();
        return usuarioId.HasValue ? await NombreUsuarioAsync(usuarioId.Value) : $"Vendedor #{vendedorId}";
    }

    private async Task<Dictionary<int, ProductoNav>> ProductosAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<int, ProductoNav>();
        return await _productoRepo.AsQueryable().AsNoTracking()
            .Where(p => lista.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
    }

    private static string NombreProducto(Dictionary<int, ProductoNav> productos, int id)
        => productos.TryGetValue(id, out var p) ? (p.Nombre ?? p.Descripcion) : $"#{id}";

    private static LineaAviso Linea(Dictionary<int, ProductoNav> productos, int id, int unidades)
        => productos.TryGetValue(id, out var p)
            ? new LineaAviso(p.Nombre ?? p.Descripcion, unidades, p.UnidadesPorEmpaque, p.NombreEmpaque)
            : new LineaAviso($"#{id}", unidades);
}
