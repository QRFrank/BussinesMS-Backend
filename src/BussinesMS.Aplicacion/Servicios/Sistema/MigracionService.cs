using BussinesMS.Aplicacion.DTOs.Sistema.Migracion;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class MigracionService : IMigracionService
{
    private readonly ICategoriaRepository _categoriaRepo;
    private readonly IFabricanteRepository _fabricanteRepo;
    private readonly IDescripcionSaborRepository _saborRepo;
    private readonly IDescripcionTamanioRepository _tamanioRepo;
    private readonly IProductoRepository _productoRepo;
    private readonly IProductoVarianteRepository _varianteRepo;
    private readonly IProveedorRepository _proveedorRepo;
    private readonly ILogger<MigracionService> _logger;
    private readonly ITipoPresentacionRepository _tipoPresentacionRepo;
    private readonly IProductoPresentacionRepository _presentacionRepo;

    public MigracionService(
    ICategoriaRepository categoriaRepo,
    IFabricanteRepository fabricanteRepo,
    IDescripcionSaborRepository saborRepo,
    IDescripcionTamanioRepository tamanioRepo,
    IProductoRepository productoRepo,
    IProductoVarianteRepository varianteRepo,
    IProveedorRepository proveedorRepo,
    ITipoPresentacionRepository tipoPresentacionRepo,   // ← nuevo
    IProductoPresentacionRepository presentacionRepo,  // ← nuevo
    ILogger<MigracionService> logger)
    {
        _categoriaRepo = categoriaRepo;
        _fabricanteRepo = fabricanteRepo;
        _saborRepo = saborRepo;
        _tamanioRepo = tamanioRepo;
        _productoRepo = productoRepo;
        _varianteRepo = varianteRepo;
        _proveedorRepo = proveedorRepo;
        _tipoPresentacionRepo = tipoPresentacionRepo;     // ← nuevo
        _presentacionRepo = presentacionRepo;         // ← nuevo
        _logger = logger;
    }

    private static string? LimpiarCampo(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var limpio = valor.Trim().TrimStart('\'').Trim();
        return string.IsNullOrWhiteSpace(limpio) ? null : limpio;
    }

    public async Task<ResultadoMigracionDto> MigrarDatosDesdeCsvAsync(IFormFile archivo)
    {
        var resultado = new ResultadoMigracionDto();

        try
        {
            _logger.LogInformation("Iniciando lectura del archivo CSV...");

            using var reader = new StreamReader(archivo.OpenReadStream());
            var contenidoCompleto = await reader.ReadToEndAsync();
            var lineas = contenidoCompleto.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lineas.Length == 0)
            {
                resultado.Success = false;
                resultado.Mensaje = "El archivo está vacío";
                return resultado;
            }

            _logger.LogInformation($"Total de líneas en archivo: {lineas.Length}");
            resultado.FilasProcesadas = lineas.Length - 1;

            // ============================================================
            // EXTRAER DATOS ÚNICOS DEL CSV
            // ============================================================
            var categoriasUnicas = new HashSet<string>();
            var fabricantesUnicos = new HashSet<string>();
            var saboresUnicos = new HashSet<string>();
            var tamaniosUnicos = new HashSet<string>();
            var productosUnicos = new HashSet<(string Nombre, string? CodigoBarras, string Categoria, string Fabricante)>();

            for (int i = 1; i < lineas.Length; i++)
            {
                var linea = lineas[i].Trim();
                if (string.IsNullOrWhiteSpace(linea)) continue;
                if (linea.Replace(";", "").Replace(" ", "").Length == 0) continue;

                var partes = linea.Split(';');

                var categoria = LimpiarCampo(partes.Length > 1 ? partes[1] : null);
                var fabricante = LimpiarCampo(partes.Length > 5 ? partes[5] : null);
                var sabor = LimpiarCampo(partes.Length > 3 ? partes[3] : null);
                var tamanio = LimpiarCampo(partes.Length > 4 ? partes[4] : null);
                var nombreProd = LimpiarCampo(partes.Length > 2 ? partes[2] : null);
                var codigoBarras = LimpiarCampo(partes.Length > 0 ? partes[0] : null);

                if (!string.IsNullOrWhiteSpace(categoria))
                    categoriasUnicas.Add(categoria.ToUpper());

                if (!string.IsNullOrWhiteSpace(fabricante))
                    fabricantesUnicos.Add(fabricante.ToUpper());

                if (!string.IsNullOrWhiteSpace(sabor))
                    saboresUnicos.Add(sabor.ToUpper());

                if (!string.IsNullOrWhiteSpace(tamanio))
                    tamaniosUnicos.Add(tamanio.ToUpper());

                if (!string.IsNullOrWhiteSpace(nombreProd))
                    productosUnicos.Add((nombreProd, codigoBarras, categoria ?? "", fabricante ?? ""));
            }

            _logger.LogInformation($"Datos únicos - Categorías: {categoriasUnicas.Count}, Fabricantes: {fabricantesUnicos.Count}, Sabores: {saboresUnicos.Count}, Tamaños: {tamaniosUnicos.Count}, Productos: {productosUnicos.Count}");

            // ============================================================
            // MIGRACIÓN DE CATEGORÍAS (planas, sin jerarquía)
            // ============================================================
            var categoriasCreadas = new Dictionary<string, int>();

            foreach (var cat in categoriasUnicas)
            {
                var existente = await _categoriaRepo.AsQueryable()
                    .FirstOrDefaultAsync(c => c.Nombre.ToUpper() == cat);

                if (existente != null)
                {
                    categoriasCreadas[cat] = existente.Id;
                    resultado.Omitidas++;
                }
                else
                {
                    var nueva = new Categoria
                    {
                        Nombre = cat,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUsuarioId = 1,
                        IsActive = true
                    };
                    await _categoriaRepo.CrearAsync(nueva);
                    categoriasCreadas[cat] = nueva.Id;
                    resultado.CategoriasCreadas.Add(cat);
                    resultado.Creadas++;
                }
            }

            _logger.LogInformation($"Categorías procesadas: {resultado.CategoriasCreadas.Count} creadas");

            // ============================================================
            // MIGRACIÓN DE FABRICANTES
            // ============================================================
            var fabricantesCreados = new Dictionary<string, int>();

            foreach (var fab in fabricantesUnicos)
            {
                if (string.IsNullOrWhiteSpace(fab)) continue;

                var existente = await _fabricanteRepo.AsQueryable()
                    .FirstOrDefaultAsync(f => f.Nombre.ToUpper() == fab);

                if (existente != null)
                {
                    fabricantesCreados[fab.ToUpper()] = existente.Id;
                    resultado.Omitidas++;
                }
                else
                {
                    var nuevo = new Fabricante
                    {
                        Nombre = fab,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUsuarioId = 1,
                        IsActive = true
                    };
                    await _fabricanteRepo.CrearAsync(nuevo);
                    fabricantesCreados[fab.ToUpper()] = nuevo.Id;
                    resultado.FabricantesCreados.Add(fab);
                    resultado.Creadas++;
                }
            }

            _logger.LogInformation($"Fabricantes procesados: {resultado.FabricantesCreados.Count} creados");

            // ============================================================
            // MIGRACIÓN DE SABORES
            // ============================================================
            foreach (var sabor in saboresUnicos)
            {
                if (string.IsNullOrWhiteSpace(sabor)) continue;

                var existente = await _saborRepo.AsQueryable()
                    .FirstOrDefaultAsync(s => s.Nombre.ToUpper() == sabor);

                if (existente != null)
                {
                    resultado.Omitidas++;
                }
                else
                {
                    var nuevo = new DescripcionSabor
                    {
                        Nombre = sabor,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUsuarioId = 1,
                        IsActive = true
                    };
                    await _saborRepo.CrearAsync(nuevo);
                    resultado.SaboresCreados.Add(sabor);
                    resultado.Creadas++;
                }
            }

            _logger.LogInformation($"Sabores procesados: {resultado.SaboresCreados.Count} creados");

            // ============================================================
            // MIGRACIÓN DE TAMAÑOS
            // ============================================================
            foreach (var tamanio in tamaniosUnicos)
            {
                if (string.IsNullOrWhiteSpace(tamanio)) continue;

                var existente = await _tamanioRepo.AsQueryable()
                    .FirstOrDefaultAsync(t => t.Nombre.ToUpper() == tamanio);

                if (existente != null)
                {
                    resultado.Omitidas++;
                }
                else
                {
                    var nuevo = new DescripcionTamanio
                    {
                        Nombre = tamanio,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUsuarioId = 1,
                        IsActive = true
                    };
                    await _tamanioRepo.CrearAsync(nuevo);
                    resultado.TamaniosCreados.Add(tamanio);
                    resultado.Creadas++;
                }
            }

            _logger.LogInformation($"Tamaños procesados: {resultado.TamaniosCreados.Count} creados");

            // ============================================================
            // MIGRACIÓN DE PRODUCTOS
            // ============================================================
            foreach (var (nombre, codigoBarras, categoria, fabricante) in productosUnicos)
            {
                if (string.IsNullOrWhiteSpace(nombre)) continue;

                var nombreUpper = nombre.ToUpper();
                var existente = await _productoRepo.AsQueryable()
                    .FirstOrDefaultAsync(p => p.Nombre.ToUpper() == nombreUpper);

                if (existente != null)
                {
                    resultado.Omitidas++;
                    continue;
                }

                if (!categoriasCreadas.TryGetValue(categoria.ToUpper(), out var catId))
                {
                    _logger.LogWarning("Categoría no encontrada: {Cat} para producto {Nombre}",
                        categoria, nombre);
                    resultado.Omitidas++;
                    continue;
                }

                int fabId = 0;
                if (!string.IsNullOrWhiteSpace(fabricante))
                    fabricantesCreados.TryGetValue(fabricante.ToUpper(), out fabId);

                var nuevo = new Producto
                {
                    Nombre = nombre,
                    CategoriaId = catId,
                    FabricanteId = fabId > 0 ? fabId : null,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUsuarioId = 1,
                    IsActive = true
                };

                var creado = await _productoRepo.CrearAsync(nuevo);
                creado.CodigoInterno = $"PROD-{creado.Id:D5}";
                await _productoRepo.ActualizarAsync(creado);

                resultado.ProductosCreados.Add(nombre);
                resultado.Creadas++;
            }

            _logger.LogInformation($"Productos procesados: {resultado.ProductosCreados.Count} creados");

            // ============================================================
            // MIGRACIÓN DE TIPOS DE PRESENTACION (Unidad, Caja, Caja2)
            // Se crean una sola vez si no existen
            // ============================================================
            var tipoUnidad = await ObtenerOCrearTipoPresentacionAsync("Unidad", 1, resultado);
            var tipoCaja = await ObtenerOCrearTipoPresentacionAsync("Caja", 2, resultado);
            var tipoCaja2 = await ObtenerOCrearTipoPresentacionAsync("Caja2", 3, resultado);

            // ============================================================
            // MIGRACIÓN DE PRODUCTO VARIANTES + PRESENTACIONES
            // ============================================================
            var productosExistentes = await _productoRepo.ObtenerTodosAsync();
            var saboresExistentes = await _saborRepo.ObtenerTodosAsync();
            var tamaniosExistentes = await _tamanioRepo.ObtenerTodosAsync();

            var productoPorNombre = productosExistentes.ToDictionary(p => p.Nombre.ToUpper(), p => p.Id);
            var productoEntidadPorNombre = productosExistentes.ToDictionary(p => p.Nombre.ToUpper(), p => p);
            var saborPorNombre = saboresExistentes.ToDictionary(s => s.Nombre.ToUpper(), s => s.Id);
            var tamanioPorNombre = tamaniosExistentes.ToDictionary(t => t.Nombre.ToUpper(), t => t.Id);

            var variantesProcesadas = new HashSet<(int ProductoId, int SaborId, int TamanioId)>();

            for (int i = 1; i < lineas.Length; i++)
            {
                var linea = lineas[i].Trim();
                if (string.IsNullOrWhiteSpace(linea)) continue;
                if (linea.Replace(";", "").Replace(" ", "").Length == 0) continue;

                var partes = linea.Split(';');
                if (partes.Length < 5) continue;

                var codigoBarras = LimpiarCampo(partes[0]);
                var nombreProducto = LimpiarCampo(partes[2]);
                var saborNombre = LimpiarCampo(partes[3]);
                var tamanioNombre = LimpiarCampo(partes[4]);
                var unidadStr = LimpiarCampo(partes.Length > 6 ? partes[6] : null);
                var cajaStr = LimpiarCampo(partes.Length > 7 ? partes[7] : null);
                var caja2Str = LimpiarCampo(partes.Length > 8 ? partes[8] : null);
                var nombreUnidad = LimpiarCampo(partes.Length > 9 ? partes[9] : null) ?? "Unidad";
                var nombreCaja = LimpiarCampo(partes.Length > 10 ? partes[10] : null) ?? "Paquete";
                var nombreCaja2 = LimpiarCampo(partes.Length > 11 ? partes[11] : null) ?? "Caja";
                var defaultReporte = LimpiarCampo(partes.Length > 12 ? partes[12] : null)?.ToLower();
                var cantidadCajaStr = LimpiarCampo(partes.Length > 13 ? partes[13] : null);
                var cantidadCaja = int.TryParse(cantidadCajaStr, out var cc) ? cc : 0;

                _logger.LogInformation("Línea {I}: producto='{Prod}' sabor='{Sabor}' tamanio='{Tam}' caja='{Caja}' caja2='{Caja2}' default='{Default}' cantidad='{Cantidad}'",
                    i, nombreProducto, saborNombre, tamanioNombre, cajaStr, caja2Str, defaultReporte, cantidadCaja);

                if (string.IsNullOrWhiteSpace(nombreProducto))
                {
                    resultado.VariantesOmitidas.Add($"Línea {i + 1}: Nombre de producto vacío");
                    resultado.Omitidas++;
                    continue;
                }

                if (!productoPorNombre.TryGetValue(nombreProducto.ToUpper(), out var productoId))
                {
                    resultado.VariantesOmitidas.Add($"Línea {i + 1}: Producto '{nombreProducto}' no encontrado en BD");
                    resultado.Omitidas++;
                    continue;
                }

                int saborId = 0, tamanioId = 0;
                if (!string.IsNullOrWhiteSpace(saborNombre))
                    saborPorNombre.TryGetValue(saborNombre.ToUpper(), out saborId);
                if (!string.IsNullOrWhiteSpace(tamanioNombre))
                    tamanioPorNombre.TryGetValue(tamanioNombre.ToUpper(), out tamanioId);

                if (saborId == 0)
                {
                    resultado.VariantesOmitidas.Add($"Línea {i + 1}: Sabor '{saborNombre}' no encontrado para '{nombreProducto}'");
                    resultado.Omitidas++;
                    continue;
                }
                if (tamanioId == 0)
                {
                    resultado.VariantesOmitidas.Add($"Línea {i + 1}: Tamaño '{tamanioNombre}' no encontrado para '{nombreProducto}'");
                    resultado.Omitidas++;
                    continue;
                }

                var claveVariante = (productoId, saborId, tamanioId);
                if (variantesProcesadas.Contains(claveVariante))
                {
                    resultado.VariantesOmitidas.Add($"Línea {i + 1}: Combinación duplicada '{nombreProducto} - {saborNombre} - {tamanioNombre}'");
                    resultado.Omitidas++;
                    continue;
                }

                if (await _varianteRepo.ExisteCombinacionAsync(productoId, saborId, tamanioId))
                {
                    variantesProcesadas.Add(claveVariante);
                    resultado.VariantesOmitidas.Add($"Línea {i + 1}: Ya existe en BD '{nombreProducto} - {saborNombre} - {tamanioNombre}'");
                    resultado.Omitidas++;
                    continue;
                }

                try
                {
                    // Verificar y limpiar código de barras duplicado
                    if (!string.IsNullOrWhiteSpace(codigoBarras))
                    {
                        if (await _varianteRepo.ExisteCodigoBarrasAsync(codigoBarras))
                        {
                            _logger.LogWarning("Línea {I}: Código de barras '{CB}' duplicado, se omite el código", i, codigoBarras);
                            codigoBarras = null;
                        }
                    }

                    var fabricanteNombre = productoEntidadPorNombre.TryGetValue(nombreProducto.ToUpper(), out var prodEnt)
                        ? prodEnt.Fabricante?.Nombre : null;

                    var nuevaVariante = new ProductoVariante
                    {
                        ProductoId = productoId,
                        SaborId = saborId,
                        TamanioId = tamanioId,
                        CodigoBarras = codigoBarras,
                        PrecioVentaUnitario = 0,
                        PrecioVentaMayoreo = 0,
                        PrecioCompra = 0,
                        CodigoAlmacen = null,
                        CantidadCaja = cantidadCaja > 0 ? cantidadCaja : null,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUsuarioId = 1,
                        IsActive = true
                    };
                    var varianteCreada = await _varianteRepo.CrearAsync(nuevaVariante);

                    var idsPorNivel = new Dictionary<int, int>();

                    // Unidad — siempre se crea
                    var presUnidad = new ProductoPresentacion
                    {
                        VarianteId = varianteCreada.Id,
                        TipoPresentacionId = tipoUnidad.Id,
                        NombrePersonalizado = nombreUnidad == "Unidad" ? null : nombreUnidad,
                        CantidadDePadre = 1,
                        PresentacionPadreId = null,
                        EsDefaultReporte = defaultReporte == null
                                              || defaultReporte == "unidad"
                                              || defaultReporte == nombreUnidad.ToLower(),
                        CodigoBarras = null,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUsuarioId = 1,
                        IsActive = true
                    };
                    var unidadCreada = await _presentacionRepo.CrearAsync(presUnidad);
                    idsPorNivel[0] = unidadCreada.Id;

                    // Caja — solo si tiene valor
                    if (!string.IsNullOrWhiteSpace(cajaStr) && int.TryParse(cajaStr, out var cajaEquiv) && cajaEquiv > 0)
                    {
                        var presCaja = new ProductoPresentacion
                        {
                            VarianteId = varianteCreada.Id,
                            TipoPresentacionId = tipoCaja.Id,
                            NombrePersonalizado = nombreCaja == "Paquete" ? null : nombreCaja,
                            CantidadDePadre = cajaEquiv,
                            PresentacionPadreId = idsPorNivel[0],
                            EsDefaultReporte = defaultReporte == "caja"
                                                  || defaultReporte == nombreCaja.ToLower()
                                                  || defaultReporte == "paquete",
                            CodigoBarras = null,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUsuarioId = 1,
                            IsActive = true
                        };
                        var cajaCreada = await _presentacionRepo.CrearAsync(presCaja);
                        idsPorNivel[1] = cajaCreada.Id;
                    }

                    // Caja2 — solo si tiene valor y existe Caja como padre
                    if (!string.IsNullOrWhiteSpace(caja2Str) && int.TryParse(caja2Str, out var caja2Equiv) && caja2Equiv > 0
                        && idsPorNivel.ContainsKey(1))
                    {
                        var presCaja2 = new ProductoPresentacion
                        {
                            VarianteId = varianteCreada.Id,
                            TipoPresentacionId = tipoCaja2.Id,
                            NombrePersonalizado = nombreCaja2 == "Caja" ? null : nombreCaja2,
                            CantidadDePadre = caja2Equiv,
                            PresentacionPadreId = idsPorNivel[1],
                            EsDefaultReporte = defaultReporte == "caja2"
                                                  || defaultReporte == nombreCaja2.ToLower(),
                            CodigoBarras = null,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUsuarioId = 1,
                            IsActive = true
                        };
                        await _presentacionRepo.CrearAsync(presCaja2);
                    }

                    variantesProcesadas.Add(claveVariante);
                    resultado.ProductoVariantesCreados.Add($"{nombreProducto} - {saborNombre} - {tamanioNombre}");
                    resultado.Creadas++;
                    _logger.LogInformation("Línea {I}: ✓ Creada '{Prod} - {Sabor} - {Tam}'",
                        i, nombreProducto, saborNombre, tamanioNombre);
                }
                catch (Exception exLinea)
                {
                    _logger.LogError("Línea {I}: ERROR al crear '{Prod} - {Sabor} - {Tam}' → {Error}",
                        i, nombreProducto, saborNombre, tamanioNombre, exLinea.Message);
                    resultado.VariantesOmitidas.Add($"Línea {i + 1}: ERROR → {exLinea.Message} | '{nombreProducto} - {saborNombre} - {tamanioNombre}'");
                    resultado.Omitidas++;
                }
            }

            if (resultado.VariantesOmitidas.Any())
            {
                _logger.LogWarning("Variantes omitidas detalle:");
                foreach (var omitida in resultado.VariantesOmitidas)
                    _logger.LogWarning("  {Omitida}", omitida);
            }

            _logger.LogInformation("Variantes procesadas: {Creadas} creadas, {Omitidas} omitidas",
                resultado.ProductoVariantesCreados.Count, resultado.VariantesOmitidas.Count);
            // ============================================================
            // MIGRACIÓN DE PROVEEDORES
            // ============================================================
            foreach (var prov in fabricantesUnicos)
            {
                if (string.IsNullOrWhiteSpace(prov)) continue;

                var existente = await _proveedorRepo.AsQueryable()
                    .FirstOrDefaultAsync(p => p.Nombre.ToUpper() == prov);

                if (existente != null)
                {
                    resultado.Omitidas++;
                }
                else
                {
                    var nuevo = new Proveedor
                    {
                        Nombre = prov,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUsuarioId = 1,
                        IsActive = true
                    };
                    await _proveedorRepo.CrearAsync(nuevo);
                    resultado.ProveedoresCreados.Add(prov);
                    resultado.Creadas++;
                }
            }

            _logger.LogInformation($"Proveedores procesados: {resultado.ProveedoresCreados.Count} creados");

            // ============================================================
            // RESULTADO FINAL
            // ============================================================
            resultado.Success = true;
            resultado.Mensaje = $"Migración completada. " +
                $"Categorías: {resultado.CategoriasCreadas.Count}, " +
                $"Fabricantes: {resultado.FabricantesCreados.Count}, " +
                $"Proveedores: {resultado.ProveedoresCreados.Count}, " +
                $"Sabores: {resultado.SaboresCreados.Count}, " +
                $"Tamaños: {resultado.TamaniosCreados.Count}, " +
                $"TiposPresentacion: {resultado.TiposPresentacionCreados.Count}, " +  // ← nuevo
                $"Productos: {resultado.ProductosCreados.Count}, " +
                $"ProductoVariantes: {resultado.ProductoVariantesCreados.Count}, " +
                $"Variantes omitidas: {resultado.VariantesOmitidas.Count}, " +
                $"Omitidos (ya existían): {resultado.Omitidas}";

            _logger.LogInformation("Migración completada: {Mensaje}", resultado.Mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar CSV: {Message}", ex.Message);
            resultado.Success = false;
            resultado.Mensaje = $"Error: {ex.Message}";
        }

        return resultado;
    }

    private async Task<TipoPresentacion> ObtenerOCrearTipoPresentacionAsync(
    string nombre, int orden, ResultadoMigracionDto resultado)
    {
        var existente = await _tipoPresentacionRepo.AsQueryable()
            .FirstOrDefaultAsync(t => t.Nombre.ToUpper() == nombre.ToUpper());

        if (existente != null)
            return existente;

        var nuevo = new TipoPresentacion
        {
            Nombre = nombre,
            Orden = orden,
            CreatedAt = DateTime.UtcNow,
            CreatedByUsuarioId = 1,
            IsActive = true
        };
        await _tipoPresentacionRepo.CrearAsync(nuevo);
        resultado.TiposPresentacionCreados.Add(nombre);
        resultado.Creadas++;
        return nuevo;
    }

}
