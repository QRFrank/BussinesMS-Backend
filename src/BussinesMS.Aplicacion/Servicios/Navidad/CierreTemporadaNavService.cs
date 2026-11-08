using System.Text.Json;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Cierre de temporada (2026-11-07, docs/navidad/logica-cierre-temporada.md). Solo admin.
// Pendientes y resumen salen de ResumenNavCalculos (los mismos números del dashboard y los reportes). El mismo cálculo sirve para el
// drawer (GET resumen) y para el POST cerrar, que rechaza con 409 si hay bloqueantes. Sin avisos de WhatsApp.
public class CierreTemporadaNavService : ICierreTemporadaNavService
{
    private const string Bloquea = "bloquea";
    private const string Alerta = "alerta";
    private const string ReferenciaCierre = "CierreTemporada";

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    private readonly ResumenNavCalculos _calc;
    private readonly ITemporadaRepository _temporadaRepo;
    private readonly ICierreTemporadaNavRepository _cierreRepo;
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly ISalidaRutaNavRepository _salidaRepo;
    private readonly IStockNavService _stock;
    private readonly IConteoNavService _conteo;
    private readonly IAbastecimientoNavService _abastecimiento;
    private readonly IRutaNavService _ruta;
    private readonly IPagoVendedorNavService _pagosVendedores;
    private readonly IAporteCapitalService _capital;
    private readonly ITemporadaService _temporadas;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ICurrentUserService _currentUser;
    private readonly VentaNavService _ventas;
    private readonly INavidadUnitOfWork _uow;
    private readonly ILogger<CierreTemporadaNavService> _logger;

    public CierreTemporadaNavService(
        ResumenNavCalculos calc,
        ITemporadaRepository temporadaRepo,
        ICierreTemporadaNavRepository cierreRepo,
        IStockAlmacenNavRepository stockRepo,
        ISalidaRutaNavRepository salidaRepo,
        IStockNavService stock,
        IConteoNavService conteo,
        IAbastecimientoNavService abastecimiento,
        IRutaNavService ruta,
        IPagoVendedorNavService pagosVendedores,
        IAporteCapitalService capital,
        ITemporadaService temporadas,
        IAlmacenAccesoNavService acceso,
        ICurrentUserService currentUser,
        VentaNavService ventas,
        INavidadUnitOfWork uow,
        ILogger<CierreTemporadaNavService> logger)
    {
        _calc = calc;
        _temporadaRepo = temporadaRepo;
        _cierreRepo = cierreRepo;
        _stockRepo = stockRepo;
        _salidaRepo = salidaRepo;
        _stock = stock;
        _conteo = conteo;
        _abastecimiento = abastecimiento;
        _ruta = ruta;
        _pagosVendedores = pagosVendedores;
        _capital = capital;
        _temporadas = temporadas;
        _acceso = acceso;
        _currentUser = currentUser;
        _ventas = ventas;
        _uow = uow;
        _logger = logger;
    }

    // ================= Resumen =================

    public async Task<CierreTemporadaResumenNavDto> ObtenerResumenAsync(int temporadaId)
    {
        try
        {
            await ExigirAdminAsync();
            var temporada = await ObtenerTemporadaAsync(temporadaId);
            return await CalcularResumenAsync(temporada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el resumen de cierre de la temporada {Id}", temporadaId);
            throw;
        }
    }

    private async Task<CierreTemporadaResumenNavDto> CalcularResumenAsync(Temporada temporada)
    {
        var t = temporada.Id;
        var abierta = temporada.Estado == EstadoTemporada.Abierta;
        _calc.IncluirMesEnCurso = true;

        var resultado = await _calc.ObtenerResultadoAsync(t, null, null);
        var capital = await _calc.ObtenerCapitalAsync(t);
        var flujo = await _calc.ObtenerFlujoEfectivoAsync(t, null, null);
        var nosDeben = await _calc.ObtenerNosDebenAsync(t);
        var debo = await _calc.ObtenerDeboAsync(t);
        var conciliacion = await _calc.ObtenerConciliacionAsync(t);

        var pendientes = new List<CierrePendienteNavDto>();

        // ---- Bloquean ----

        // Tiendas con conteo diario con una apertura sin su cierre (el último conteo es una apertura). Solo en la temporada abierta
        var tiendas = abierta
            ? (await _temporadaRepo.ObtenerAlmacenesConteoAsync(new[] { t })).Select(a => a.AlmacenId).Distinct().ToList()
            : new List<int>();
        var nombresAlmacen = await _ventas.ObtenerNombresAlmacenAsync(tiendas);
        var turnos = new List<CierrePendienteDetalleNavDto>();
        foreach (var id in tiendas)
        {
            var estado = await _conteo.ObtenerEstadoAsync(id, null);
            if (!estado.RequiereConteo || estado.UltimoTipo != (int)TipoConteoInventario.Apertura) continue;
            var nombre = nombresAlmacen.GetValueOrDefault(id, $"#{id}");
            var fecha = estado.UltimaFecha.HasValue ? $" desde el {MensajesAvisosNav.Dia(estado.UltimaFecha.Value)}" : string.Empty;
            turnos.Add(new CierrePendienteDetalleNavDto { Id = id, Nombre = nombre, Texto = $"{nombre}: apertura sin cierre{fecha}" });
        }
        Agregar(pendientes, "turnosAbiertos", Bloquea, "Turnos abiertos (apertura sin cierre)", null, null, turnos);

        // Cierres de tienda sin su cierre de caja (el cajasSinEntregar del flujo de efectivo)
        Agregar(pendientes, "cajasSinEntregar", Bloquea, "Cajas sin entregar", flujo.CajasSinEntregar.Monto, null,
            flujo.CajasSinEntregar.Tiendas.Select(c => new CierrePendienteDetalleNavDto
            {
                Id = c.AlmacenId,
                Nombre = c.AlmacenNombre,
                Monto = c.EfectivoEsperado,
                Texto = $"{c.AlmacenNombre} {MensajesAvisosNav.Bs(c.EfectivoEsperado)}"
            }).ToList());

        // Salidas de ruta no cerradas
        var salidas = await _salidaRepo.AsQueryable().AsNoTracking()
            .Where(s => s.IsActive && s.TemporadaId == t && s.Estado == EstadoSalidaRuta.Abierta)
            .OrderBy(s => s.FechaApertura)
            .Select(s => new { s.Id, s.FechaApertura, UsuarioId = s.Vendedor != null ? s.Vendedor.UsuarioId : 0 })
            .ToListAsync();
        var nombresUsuario = await _ventas.ObtenerNombresUsuarioAsync(salidas.Select(s => s.UsuarioId));
        Agregar(pendientes, "salidasAbiertas", Bloquea, "Salidas de ruta abiertas", null, null,
            salidas.Select(s =>
            {
                var nombre = nombresUsuario.GetValueOrDefault(s.UsuarioId, string.Empty);
                return new CierrePendienteDetalleNavDto
                {
                    Id = s.Id,
                    Nombre = nombre,
                    Texto = $"{nombre}: salida #{s.Id} abierta desde el {MensajesAvisosNav.Dia(s.FechaApertura)}"
                };
            }).ToList());

        // Vendedores de ruta con saldo por rendir (nosDeben.vendedoresRuta)
        Agregar(pendientes, "rendicionesPendientes", Bloquea, "Vendedores de ruta con saldo por rendir",
            nosDeben.VendedoresRuta.PorVendedor.Where(v => v.Saldo > 0).Sum(v => v.Saldo), null,
            nosDeben.VendedoresRuta.PorVendedor.Where(v => v.Saldo > 0).Select(v => new CierrePendienteDetalleNavDto
            {
                Id = v.VendedorId,
                Nombre = v.VendedorNombre,
                Monto = v.Saldo,
                Texto = $"{v.VendedorNombre} {MensajesAvisosNav.Bs(v.Saldo)}"
            }).ToList());

        // Ventas de tienda a crédito con saldo, y la parte entregada de reservas con saldo (nosDeben.clientes), por cliente
        var ventasConSaldo = await _calc.ObtenerVentasTiendaConSaldoAsync(t);
        Agregar(pendientes, "creditosClientes", Bloquea, "Ventas a crédito con saldo", nosDeben.Clientes.Total, null,
            ventasConSaldo
                .GroupBy(v => new { v.ClienteId, Nombre = v.ClienteId == null ? "Sin cliente" : v.ClienteNombre ?? string.Empty })
                .Select(g => new CierrePendienteDetalleNavDto
                {
                    Id = g.Key.ClienteId,
                    Nombre = g.Key.Nombre,
                    Monto = g.Sum(v => v.Saldo),
                    Cantidad = g.Count(),
                    Texto = $"{g.Key.Nombre} {MensajesAvisosNav.Bs(g.Sum(v => v.Saldo))}",
                    Ventas = g.OrderBy(v => v.Fecha).ThenBy(v => v.Id).Select(v => new CierrePendienteVentaNavDto
                    {
                        VentaId = v.Id,
                        NroVenta = v.NroVenta,
                        Fecha = v.Fecha,
                        Tipo = v.Tipo == TipoVentaNav.Reserva ? "Reserva" : "Credito",
                        Saldo = v.Saldo
                    }).ToList()
                })
                .OrderByDescending(d => d.Monto)
                .ToList());

        // Reservas con producto sin entregar
        var reservas = await _calc.ObtenerReservasPendientesAsync(t);
        var reservasLista = (await _calc.CargarVentasAsync(t)).Where(v => v.Tipo == TipoVentaNav.Reserva && v.Peso < 1m).OrderBy(v => v.Fecha).ThenBy(v => v.Id).ToList();
        Agregar(pendientes, "reservasPendientes", Bloquea, "Reservas sin entregar", reservas.TotalReservado, null,
            reservasLista.Select(v =>
            {
                var nombre = v.ClienteNombre ?? "Sin cliente";
                var reservado = v.MontoNeto - ResumenNavCalculos.Parte(v.MontoNeto, v.Peso);
                return new CierrePendienteDetalleNavDto
                {
                    Id = v.Id,
                    Nombre = nombre,
                    Monto = reservado,
                    Texto = $"Reserva #{v.NroVenta} · {nombre} {MensajesAvisosNav.Bs(reservado)}"
                };
            }).ToList());

        // Deuda con proveedores (debo)
        Agregar(pendientes, "deudaProveedores", Bloquea, "Deuda con proveedores", debo.Total, null,
            debo.PorCodigo.Where(d => d.Saldo > 0).Select(d =>
            {
                var nombre = d.Codigo == null ? d.ProveedorNombre : $"{d.ProveedorNombre} · {d.Codigo}";
                return new CierrePendienteDetalleNavDto
                {
                    Id = d.ProveedorId,
                    Nombre = nombre,
                    Monto = d.Saldo,
                    Texto = $"{nombre} {MensajesAvisosNav.Bs(d.Saldo)}"
                };
            }).ToList());

        // Inversores (no capital propio) con capital sin devolver o comisión sin pagar
        var aportes = (await _capital.ObtenerResumenAsync(t)).Aportes
            .Where(a => !a.EsCapitalPropio && (a.SaldoCapital > 0 || a.SaldoComision > 0))
            .ToList();
        Agregar(pendientes, "inversores", Bloquea, "Inversores sin pagar", aportes.Sum(a => a.SaldoCapital + a.SaldoComision), null,
            aportes.Select(a => new CierrePendienteDetalleNavDto
            {
                Id = a.AporteCapitalId,
                Nombre = a.InversorNombre,
                Monto = a.SaldoCapital + a.SaldoComision,
                Texto = $"{a.InversorNombre}: capital {MensajesAvisosNav.Bs(a.SaldoCapital)} + comisión {MensajesAvisosNav.Bs(a.SaldoComision)}"
            }).ToList());

        // ---- Alertan ----

        // Vendedores de ruta con ventas y sin pago de comisión (se decide al pagar: sin monto)
        var reporte = await _pagosVendedores.ObtenerReporteAsync(new ReporteVentasVendedorFiltroDto { TemporadaId = t });
        var comisiones = await _calc.ConstruirComisionesAsync(t, reporte);
        Agregar(pendientes, "comisionesRuta", Alerta, "Comisiones de ruta sin pagar (a definir)", null, null,
            comisiones.Pendientes.Select(c => new CierrePendienteDetalleNavDto
            {
                Id = c.VendedorId,
                Nombre = c.VendedorNombre,
                Cantidad = c.Unidades,
                Texto = $"{c.VendedorNombre} {c.Unidades} u vendidas"
            }).ToList());

        // Sueldos de tienda pendientes
        var sueldos = _calc.ConstruirSueldos(await _pagosVendedores.ObtenerResumenSueldosAsync(t, null, true));
        Agregar(pendientes, "sueldosTienda", Alerta, "Sueldos de tienda pendientes", sueldos.TotalDebe, null,
            sueldos.PorVendedor.Select(s => new CierrePendienteDetalleNavDto
            {
                Id = s.VendedorId,
                Nombre = s.VendedorNombre,
                Monto = s.Debe,
                Texto = $"{s.VendedorNombre} {MensajesAvisosNav.Bs(s.Debe)}"
            }).ToList());

        // Stock que queda en los almacenes: unidades y Bs a costo promedio. Al cerrar pasa a pérdida
        var stockPorAlmacen = await _stockRepo.AsQueryable().AsNoTracking()
            .Where(s => s.IsActive && s.TemporadaId == t && s.Cantidad > 0)
            .GroupBy(s => s.AlmacenId)
            .Select(g => new { AlmacenId = g.Key, Unidades = g.Sum(s => s.Cantidad) })
            .ToListAsync();
        var nombresStock = await _ventas.ObtenerNombresAlmacenAsync(stockPorAlmacen.Select(s => s.AlmacenId));
        var stockDetalle = new List<CierrePendienteDetalleNavDto>();
        foreach (var s in stockPorAlmacen)
        {
            var nombre = nombresStock.GetValueOrDefault(s.AlmacenId, $"#{s.AlmacenId}");
            var valor = await _calc.ObtenerStockValorizadoAsync(t, s.AlmacenId);
            stockDetalle.Add(new CierrePendienteDetalleNavDto
            {
                Id = s.AlmacenId,
                Nombre = nombre,
                Monto = valor,
                Cantidad = s.Unidades,
                Texto = $"{nombre} {s.Unidades} u · {MensajesAvisosNav.Bs(valor)}"
            });
        }
        var stockUnidades = stockPorAlmacen.Sum(s => s.Unidades);
        var stockMonto = capital.StockValorizado;
        Agregar(pendientes, "stockSobrante", Alerta, "Stock sobrante (al cerrar pasa a pérdida)", stockMonto, stockUnidades,
            stockDetalle.OrderBy(d => d.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList());

        // Pedidos con mercadería todavía sin recibir
        var faltantes = (await _abastecimiento.ObtenerFaltantesAsync(new AbastecimientoNavFiltroDto { TemporadaId = t }))
            .Where(f => f.Faltante > 0).ToList();
        var porRecibir = conciliacion.RealLineas.FirstOrDefault(l => l.Clave == "mercaderiaPorRecibir")?.Monto ?? 0m;
        Agregar(pendientes, "mercaderiaPorRecibir", Alerta, "Mercadería por recibir", porRecibir, faltantes.Sum(f => f.Faltante),
            faltantes
                .GroupBy(f => new { f.ProveedorId, Nombre = f.Codigo == null ? f.ProveedorNombre : $"{f.ProveedorNombre} · {f.Codigo}" })
                .Select(g => new CierrePendienteDetalleNavDto
                {
                    Id = g.Key.ProveedorId,
                    Nombre = g.Key.Nombre,
                    Cantidad = g.Sum(f => f.Faltante),
                    Texto = $"{g.Key.Nombre} {g.Sum(f => f.Faltante)} u"
                })
                .OrderByDescending(d => d.Cantidad)
                .ToList());

        // ---- Resumen (sección 4) ----

        var lineas = resultado.Lineas.ToList();
        lineas.Add(new DashboardResultadoLineaNavDto
        {
            Clave = "stockSobranteAPerdida",
            Nombre = "Stock sobrante a pérdida",
            Signo = -1,
            Monto = abierta ? stockMonto : 0m,
            Origen = "Stock que queda en los almacenes × costo promedio: se da de baja al cerrar (en una temporada cerrada ya está en stockSobranteCierre)"
        });
        var ganancia = lineas.Sum(l => l.Signo * l.Monto);

        var debemos = new CierreDebemosNavDto
        {
            Proveedores = debo.Total,
            Inversores = capital.DeudaInversores,
            SueldosTienda = capital.SueldosPendientes
        };
        debemos.Total = debemos.Proveedores + debemos.Inversores + debemos.SueldosTienda;
        var plata = new CierrePlataNavDto { EfectivoNeto = flujo.Neto, NosDeben = nosDeben.Total, Debemos = debemos };
        plata.TeQueda = plata.EfectivoNeto + plata.NosDeben - debemos.Total;

        var bloqueantes = pendientes.Count(p => p.Nivel == Bloquea);
        return new CierreTemporadaResumenNavDto
        {
            Temporada = MapearTemporada(temporada),
            Cerrada = !abierta,
            PuedeCerrar = abierta && bloqueantes == 0,
            CantidadBloqueantes = bloqueantes,
            CantidadAlertas = pendientes.Count - bloqueantes,
            Pendientes = pendientes,
            Resultado = new CierreResultadoNavDto { Lineas = lineas, Ganancia = ganancia, Advertencias = resultado.Advertencias },
            Devengados = resultado.Devengados,
            GananciaNeta = ganancia - resultado.Devengados.Total,
            Plata = plata,
            CapitalPropio = capital.CapitalPropio,
            DiferenciaConciliacion = conciliacion.Diferencia,
            CalculadoEn = BoliviaTimeZone.Now()
        };
    }

    // Solo se agrega si tiene algo (detalle o monto)
    private static void Agregar(List<CierrePendienteNavDto> lista, string clave, string nivel, string titulo, decimal? monto, int? unidades,
        List<CierrePendienteDetalleNavDto> detalle)
    {
        if (detalle.Count == 0 && (monto ?? 0m) <= 0m && (unidades ?? 0) <= 0) return;
        lista.Add(new CierrePendienteNavDto
        {
            Clave = clave,
            Nivel = nivel,
            Titulo = titulo,
            Monto = monto.HasValue ? ResumenNavCalculos.Redondear(monto.Value) : null,
            Cantidad = detalle.Count,
            Unidades = unidades,
            Detalle = detalle
        });
    }

    // ================= Cerrar =================

    public async Task<CierreTemporadaResultadoNavDto> CerrarAsync(int temporadaId, CerrarTemporadaNavDto? dto)
    {
        try
        {
            await ExigirAdminAsync();
            var temporada = await ObtenerTemporadaAsync(temporadaId);
            if (temporada.Estado == EstadoTemporada.Cerrada)
                throw new ExcepcionDominio("La temporada ya está cerrada", 409, "TEMPORADA_CERRADA");

            if (dto?.ConfirmacionNombre != null
                && !string.Equals(dto.ConfirmacionNombre.Trim(), temporada.Nombre.Trim(), StringComparison.CurrentCultureIgnoreCase))
                throw new ExcepcionDominio("El nombre escrito no coincide con el de la temporada", 400, "CONFIRMACION_INVALIDA");

            var resumen = await CalcularResumenAsync(temporada);
            if (resumen.CantidadBloqueantes > 0)
                throw new TemporadaConPendientesException(resumen.Pendientes);

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var hoy = BoliviaTimeZone.Now().Date;
            var stockLinea = resumen.Resultado.Lineas.First(l => l.Clave == "stockSobranteAPerdida");
            var stockUnidades = resumen.Pendientes.FirstOrDefault(p => p.Clave == "stockSobrante")?.Unidades ?? 0;

            // La foto queda como "cerrada"
            resumen.Temporada.Estado = (int)EstadoTemporada.Cerrada;
            resumen.Temporada.FechaCierre = hoy;
            resumen.Cerrada = true;
            resumen.PuedeCerrar = false;

            var stock = await _stockRepo.AsQueryable().AsNoTracking()
                .Where(s => s.IsActive && s.TemporadaId == temporadaId && s.Cantidad > 0)
                .Select(s => new { s.AlmacenId, s.ProductoId, s.Cantidad })
                .ToListAsync();
            var costos = await _stock.ObtenerCostosPromedioAsync(temporadaId, stock.Select(s => s.ProductoId));

            CierreTemporadaNav cierre;
            await _uow.BeginTransactionAsync();
            try
            {
                // 1. Stock sobrante a pérdida: una salida "Cierre de temporada" por almacén y producto; la cantidad queda en 0
                foreach (var s in stock)
                {
                    var costo = ResumenNavCalculos.Redondear(costos.GetValueOrDefault(s.ProductoId));
                    await _stock.RegistrarSalidaAsync(temporadaId, s.AlmacenId, s.ProductoId, s.Cantidad,
                        TipoMovimientoNav.CierreTemporada, ReferenciaCierre, temporadaId,
                        $"Cierre de temporada: stock sobrante a pérdida (costo promedio Bs {costo:0.00})", respetarBloqueo: false);
                }

                // 2. Foto del resultado
                cierre = await _cierreRepo.CrearAsync(new CierreTemporadaNav
                {
                    TemporadaId = temporadaId,
                    FechaCierre = hoy,
                    UsuarioId = usuarioId,
                    CreatedByUsuarioId = usuarioId,
                    Ganancia = resumen.Resultado.Ganancia,
                    Devengados = resumen.Devengados.Total,
                    GananciaNeta = resumen.GananciaNeta,
                    DeudasIncobrables = resumen.Resultado.Lineas.FirstOrDefault(l => l.Clave == "deudasIncobrables")?.Monto ?? 0m,
                    StockSobranteUnidades = stockUnidades,
                    StockSobranteMonto = stockLinea.Monto,
                    EfectivoNeto = resumen.Plata.EfectivoNeto,
                    NosDeben = resumen.Plata.NosDeben,
                    Debemos = resumen.Plata.Debemos.Total,
                    TeQueda = resumen.Plata.TeQueda,
                    CapitalPropio = resumen.CapitalPropio,
                    ResumenJson = JsonSerializer.Serialize(resumen, Json)
                });

                // 3. Cerrada, sin reapertura
                var entidad = await _temporadaRepo.ObtenerPorIdAsync(temporadaId)
                    ?? throw new EntidadNoEncontradaException("Temporada", temporadaId);
                entidad.Estado = EstadoTemporada.Cerrada;
                entidad.FechaCierre = hoy;
                entidad.UpdatedAt = DateTime.UtcNow;
                entidad.UpdatedByUsuarioId = usuarioId;
                await _temporadaRepo.ActualizarAsync(entidad);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Temporada cerrada: {Id}. Stock dado de baja: {Unidades} u, Bs {Monto}", temporadaId, stockUnidades, stockLinea.Monto);

            return new CierreTemporadaResultadoNavDto
            {
                Temporada = await _temporadas.ObtenerPorIdAsync(temporadaId) ?? new TemporadaDto(),
                Cierre = await MapearCierreAsync(cierre)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cerrar la temporada {Id}", temporadaId);
            throw;
        }
    }

    // ================= Foto =================

    public async Task<CierreTemporadaFotoNavDto> ObtenerCierreAsync(int temporadaId)
    {
        try
        {
            await ExigirAdminAsync();
            await ObtenerTemporadaAsync(temporadaId);
            var cierre = await _cierreRepo.ObtenerPorTemporadaAsync(temporadaId)
                ?? throw new ExcepcionDominio("La temporada no tiene cierre registrado", 404, "CIERRE_NO_ENCONTRADO");
            return await MapearCierreAsync(cierre);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el cierre de la temporada {Id}", temporadaId);
            throw;
        }
    }

    private async Task<CierreTemporadaFotoNavDto> MapearCierreAsync(CierreTemporadaNav c)
    {
        var nombres = await _ventas.ObtenerNombresUsuarioAsync(new[] { c.UsuarioId });
        return new CierreTemporadaFotoNavDto
        {
            Id = c.Id,
            TemporadaId = c.TemporadaId,
            FechaCierre = c.FechaCierre,
            UsuarioId = c.UsuarioId,
            UsuarioNombre = nombres.GetValueOrDefault(c.UsuarioId, string.Empty),
            Ganancia = c.Ganancia,
            Devengados = c.Devengados,
            GananciaNeta = c.GananciaNeta,
            DeudasIncobrables = c.DeudasIncobrables,
            StockSobranteUnidades = c.StockSobranteUnidades,
            StockSobranteMonto = c.StockSobranteMonto,
            EfectivoNeto = c.EfectivoNeto,
            NosDeben = c.NosDeben,
            Debemos = c.Debemos,
            TeQueda = c.TeQueda,
            CapitalPropio = c.CapitalPropio,
            Resumen = string.IsNullOrWhiteSpace(c.ResumenJson) ? null : JsonSerializer.Deserialize<CierreTemporadaResumenNavDto>(c.ResumenJson, Json),
            CreatedAt = BoliviaTimeZone.ToLocal(c.CreatedAt)
        };
    }

    // ================= Comunes =================

    private async Task ExigirAdminAsync()
    {
        if (!await _acceso.EsAdminAsync())
            throw new ExcepcionDominio("Solo el admin puede cerrar la temporada", 403, "SIN_PERMISO");
    }

    private async Task<Temporada> ObtenerTemporadaAsync(int temporadaId)
    {
        var temporada = await _temporadaRepo.ObtenerPorIdAsync(temporadaId);
        if (temporada == null || !temporada.IsActive)
            throw new EntidadNoEncontradaException("Temporada", temporadaId);
        return temporada;
    }

    private static DashboardTemporadaNavDto MapearTemporada(Temporada t) => new()
    {
        Id = t.Id,
        Nombre = t.Nombre,
        Anio = t.Anio,
        FechaInicio = t.FechaInicio,
        Estado = (int)t.Estado,
        FechaCierre = t.FechaCierre
    };
}
