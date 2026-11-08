using System.Globalization;
using System.Text;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Excepciones;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductoNav = BussinesMS.Dominio.Entidades.Navidad.Producto;
using ProveedorNav = BussinesMS.Dominio.Entidades.Navidad.Proveedor;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

/// <summary>
/// Importa productos navideños desde el Excel de carga (hoja "Productos"): crea proveedores y categorías que falten
/// y los productos de la temporada abierta. Todo o nada. Contrato: docs/navidad/contratos-importacion.md.
/// </summary>
public class ImportacionNavService : IImportacionNavService
{
    private const string HojaProductos = "Productos";

    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly ICategoriaProductoNavRepository _categoriaRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ILogger<ImportacionNavService> _logger;

    public ImportacionNavService(
        IProveedorNavRepository proveedorRepo,
        IProductoNavRepository productoRepo,
        ICategoriaProductoNavRepository categoriaRepo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ILogger<ImportacionNavService> logger)
    {
        _proveedorRepo = proveedorRepo;
        _productoRepo = productoRepo;
        _categoriaRepo = categoriaRepo;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _logger = logger;
    }

    private sealed class Fila
    {
        public int Numero { get; set; }
        public string Proveedor { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string? Alias { get; set; }
        public decimal PrecioCatalogo { get; set; }
        public decimal PrecioCompra { get; set; }
        public int? UnidadesPorEmpaque { get; set; }
        public string? NombreEmpaque { get; set; }
    }

    public async Task<ImportacionProductosResultadoNavDto> ImportarProductosAsync(Stream archivo)
    {
        try
        {
            Temporada temporada;
            try
            {
                temporada = await _temporadaActual.ObtenerAbiertaAsync();
            }
            catch (ExcepcionDominio ex) when (ex.CodigoError == "SIN_TEMPORADA_ABIERTA")
            {
                throw new ValidacionException("No hay temporada abierta: creá la temporada antes de importar los productos");
            }

            var resultado = new ImportacionProductosResultadoNavDto();
            var filas = LeerFilas(archivo, resultado.Errores);

            if (filas.Count == 0 && resultado.Errores.Count == 0)
                throw new ValidacionException("La hoja \"Productos\" no tiene filas con datos");

            // Estado actual de la BD
            var proveedores = await _proveedorRepo.AsQueryable().AsNoTracking()
                .Where(p => p.TemporadaId == temporada.Id && p.IsActive).ToListAsync();
            var proveedorPorNombre = proveedores.GroupBy(p => Clave(p.Nombre)).ToDictionary(g => g.Key, g => g.First());

            var productosExistentes = await _productoRepo.AsQueryable().AsNoTracking()
                .Where(p => p.TemporadaId == temporada.Id && p.IsActive)
                .Select(p => new { p.ProveedorId, p.Descripcion, p.Nombre })
                .ToListAsync();
            var descripcionesExistentes = new HashSet<(int, string)>(productosExistentes.Select(p => (p.ProveedorId, Clave(p.Descripcion))));
            var aliasExistentes = new HashSet<string>(productosExistentes.Where(p => p.Nombre != null).Select(p => Clave(p.Nombre!)));

            var categorias = await _categoriaRepo.AsQueryable().AsNoTracking().ToListAsync();
            var categoriaPorNombre = categorias.GroupBy(c => Clave(c.Nombre)).ToDictionary(g => g.Key, g => g.First());

            // Validación contra la BD y dentro del archivo
            var aCrear = new List<Fila>();
            var vistosDescripcion = new Dictionary<(string, string), int>();
            var vistosAlias = new Dictionary<string, int>();
            foreach (var f in filas)
            {
                if (categoriaPorNombre.TryGetValue(Clave(f.Categoria), out var cat) && !cat.IsActive)
                {
                    resultado.Errores.Add(Error(f.Numero, $"La categoría '{f.Categoria}' existe pero está inactiva"));
                    continue;
                }

                var claveDesc = (Clave(f.Proveedor), Clave(f.Descripcion));
                if (vistosDescripcion.TryGetValue(claveDesc, out var filaPrevia))
                {
                    resultado.Errores.Add(Error(f.Numero, $"Producto repetido en el archivo (igual al de la fila {filaPrevia})"));
                    continue;
                }
                vistosDescripcion[claveDesc] = f.Numero;

                if (proveedorPorNombre.TryGetValue(Clave(f.Proveedor), out var prov)
                    && descripcionesExistentes.Contains((prov.Id, Clave(f.Descripcion))))
                {
                    resultado.Omitidos.Add(new ImportacionOmitidoNavDto { Fila = f.Numero, Motivo = "El producto ya existe para ese proveedor" });
                    continue;
                }

                if (f.Alias != null)
                {
                    var claveAlias = Clave(f.Alias);
                    if (aliasExistentes.Contains(claveAlias))
                    {
                        resultado.Errores.Add(Error(f.Numero, $"Ya existe un producto en la temporada con el nombre '{f.Alias}'"));
                        continue;
                    }
                    if (vistosAlias.TryGetValue(claveAlias, out var filaAlias))
                    {
                        resultado.Errores.Add(Error(f.Numero, $"El nombre '{f.Alias}' está repetido en el archivo (fila {filaAlias})"));
                        continue;
                    }
                    vistosAlias[claveAlias] = f.Numero;
                }

                aCrear.Add(f);
            }

            if (resultado.Errores.Count > 0)
            {
                resultado.Errores = resultado.Errores.OrderBy(e => e.Fila).ToList();
                resultado.Omitidos.Clear();
                return resultado;
            }

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var f in aCrear)
                {
                    if (!categoriaPorNombre.TryGetValue(Clave(f.Categoria), out var categoria))
                    {
                        categoria = await _categoriaRepo.CrearAsync(new CategoriaProductoNav { Nombre = f.Categoria });
                        categoriaPorNombre[Clave(f.Categoria)] = categoria;
                        resultado.CategoriasCreadas++;
                    }

                    if (!proveedorPorNombre.TryGetValue(Clave(f.Proveedor), out var proveedor))
                    {
                        proveedor = await _proveedorRepo.CrearAsync(new ProveedorNav
                        {
                            TemporadaId = temporada.Id,
                            Nombre = f.Proveedor,
                            UsaCodigosCliente = false,
                            TrabajaConPedido = false
                        });
                        proveedorPorNombre[Clave(f.Proveedor)] = proveedor;
                        resultado.ProveedoresCreados++;
                    }

                    await _productoRepo.CrearAsync(new ProductoNav
                    {
                        TemporadaId = temporada.Id,
                        ProveedorId = proveedor.Id,
                        CategoriaProductoId = categoria.Id,
                        Descripcion = f.Descripcion,
                        Nombre = f.Alias,
                        PrecioCompraUnidad = f.PrecioCompra,
                        PrecioCatalogo = f.PrecioCatalogo,
                        UnidadesPorEmpaque = f.UnidadesPorEmpaque,
                        NombreEmpaque = f.NombreEmpaque
                    });
                    resultado.ProductosCreados++;
                }

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation(
                "Importación de productos: {Productos} productos, {Proveedores} proveedores y {Categorias} categorías creados, {Omitidos} omitidos (temporada {TemporadaId})",
                resultado.ProductosCreados, resultado.ProveedoresCreados, resultado.CategoriasCreadas, resultado.Omitidos.Count, temporada.Id);

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al importar productos desde Excel");
            throw;
        }
    }

    // ---------- Lectura del Excel ----------

    private static List<Fila> LeerFilas(Stream archivo, List<ImportacionErrorNavDto> errores)
    {
        XLWorkbook libro;
        try
        {
            libro = new XLWorkbook(archivo);
        }
        catch (Exception)
        {
            throw new ValidacionException("El archivo no es un Excel (.xlsx) válido");
        }

        using (libro)
        {
            var hoja = libro.Worksheets.FirstOrDefault(h => Clave(h.Name) == Clave(HojaProductos))
                ?? throw new ValidacionException("El archivo no tiene la hoja \"Productos\"");

            var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 0;
            var ultimaColumna = hoja.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (ultimaFila < 1 || ultimaColumna < 1)
                throw new ValidacionException("La hoja \"Productos\" está vacía");

            // Ubicar columnas por el texto del encabezado (fila 1)
            var encabezados = new Dictionary<int, string>();
            for (var c = 1; c <= ultimaColumna; c++)
                encabezados[c] = Normalizar(hoja.Cell(1, c).GetString());

            int Buscar(Func<string, bool> regla) =>
                encabezados.Where(e => regla(e.Value)).Select(e => e.Key).FirstOrDefault();

            var colProveedor = Buscar(h => h == "proveedor");
            var colCategoria = Buscar(h => h.StartsWith("categoria"));
            var colDescripcion = Buscar(h => h.StartsWith("descripcion"));
            var colAlias = Buscar(h => h.Contains("alias"));
            var colCatalogo = Buscar(h => h.StartsWith("precio catalogo"));
            var colCompra = Buscar(h => h.StartsWith("precio compra"));
            var colTiene = Buscar(h => h.StartsWith("tiene"));
            var colNombreEmpaque = Buscar(h => h.StartsWith("nombre empaque"));
            var colUnidades = Buscar(h => h.StartsWith("unidades por empaque"));

            var faltantes = new List<string>();
            if (colProveedor == 0) faltantes.Add("Proveedor");
            if (colCategoria == 0) faltantes.Add("Categoria");
            if (colDescripcion == 0) faltantes.Add("Descripcion");
            if (colAlias == 0) faltantes.Add("Nombre / alias");
            if (colCatalogo == 0) faltantes.Add("Precio catalogo");
            if (colCompra == 0) faltantes.Add("Precio compra");
            if (colTiene == 0) faltantes.Add("Tiene java/caja?");
            if (colNombreEmpaque == 0) faltantes.Add("Nombre empaque");
            if (colUnidades == 0) faltantes.Add("Unidades por empaque");
            if (faltantes.Count > 0)
                throw new ValidacionException($"En la hoja \"Productos\" faltan las columnas: {string.Join(", ", faltantes)}");

            var filas = new List<Fila>();
            for (var r = 2; r <= ultimaFila; r++)
            {
                var proveedor = Texto(hoja.Cell(r, colProveedor));
                var categoria = Texto(hoja.Cell(r, colCategoria));
                var descripcion = Texto(hoja.Cell(r, colDescripcion));
                var catalogoTexto = Texto(hoja.Cell(r, colCatalogo));

                // Fila vacía: se salta
                if (proveedor == "" && categoria == "" && descripcion == "" && catalogoTexto == "")
                    continue;

                var errs = new List<string>();
                if (proveedor == "") errs.Add("Falta el proveedor");
                else if (proveedor.Length > 150) errs.Add("El proveedor no puede superar 150 caracteres");
                if (categoria == "") errs.Add("Falta la categoría");
                else if (categoria.Length > 100) errs.Add("La categoría no puede superar 100 caracteres");
                if (descripcion == "") errs.Add("Falta la descripción");
                else if (descripcion.Length > 150) errs.Add("La descripción no puede superar 150 caracteres");

                var alias = Texto(hoja.Cell(r, colAlias));
                if (alias.Length > 150) errs.Add("El nombre / alias no puede superar 150 caracteres");

                var catalogo = Decimal(hoja.Cell(r, colCatalogo), "precio catálogo", errs);
                var compra = Decimal(hoja.Cell(r, colCompra), "precio de compra", errs);

                int? unidades = null;
                string? nombreEmpaque = null;
                var tiene = Normalizar(Texto(hoja.Cell(r, colTiene)));
                if (tiene == "si")
                {
                    nombreEmpaque = Texto(hoja.Cell(r, colNombreEmpaque));
                    if (nombreEmpaque == "") errs.Add("Falta el nombre del empaque (Java o Caja)");
                    else if (nombreEmpaque.Length > 20) errs.Add("El nombre del empaque no puede superar 20 caracteres");

                    var unidadesNum = Decimal(hoja.Cell(r, colUnidades), "unidades por empaque", errs, positivo: false);
                    if (unidadesNum.HasValue)
                    {
                        if (unidadesNum.Value != Math.Truncate(unidadesNum.Value) || unidadesNum.Value <= 1)
                            errs.Add("Las unidades por empaque deben ser un entero mayor a 1");
                        else
                            unidades = (int)unidadesNum.Value;
                    }
                }
                else if (tiene != "no")
                    errs.Add("En \"Tiene java/caja?\" hay que poner Si o No");

                if (errs.Count > 0)
                {
                    errores.Add(Error(r, string.Join("; ", errs)));
                    continue;
                }

                filas.Add(new Fila
                {
                    Numero = r,
                    Proveedor = proveedor,
                    Categoria = categoria,
                    Descripcion = descripcion,
                    Alias = alias == "" ? null : alias,
                    PrecioCatalogo = catalogo!.Value,
                    PrecioCompra = compra!.Value,
                    UnidadesPorEmpaque = unidades,
                    NombreEmpaque = unidades.HasValue ? nombreEmpaque : null
                });
            }

            return filas;
        }
    }

    // Valor calculado de la celda (si es fórmula, ClosedXML la evalúa); siempre como texto recortado
    private static string Texto(IXLCell celda)
    {
        if (celda.IsEmpty()) return "";
        try
        {
            var v = celda.Value;
            if (v.IsBlank) return "";
            if (v.IsNumber) return v.GetNumber().ToString(CultureInfo.InvariantCulture);
            if (v.IsText) return v.GetText().Trim();
            if (v.IsBoolean) return v.GetBoolean() ? "Si" : "No";
            if (v.IsError) return "#ERROR";
            return celda.GetString().Trim();
        }
        catch (Exception)
        {
            return "#ERROR";
        }
    }

    // Número (> 0 por defecto) redondeado a 2 decimales; acepta coma o punto decimal si vino como texto
    private static decimal? Decimal(IXLCell celda, string nombre, List<string> errs, bool positivo = true)
    {
        decimal valor;
        try
        {
            if (celda.IsEmpty() || celda.Value.IsBlank)
            {
                errs.Add($"Falta el {nombre}");
                return null;
            }
            if (celda.Value.IsNumber) valor = (decimal)celda.Value.GetNumber();
            else
            {
                var t = Texto(celda).Replace(" ", "");
                if (t.Contains(',') && !t.Contains('.')) t = t.Replace(',', '.');
                if (!decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out valor))
                {
                    errs.Add($"El {nombre} no es un número válido");
                    return null;
                }
            }
        }
        catch (Exception)
        {
            errs.Add($"El {nombre} no es un número válido");
            return null;
        }

        valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        if (positivo && valor <= 0)
        {
            errs.Add($"El {nombre} debe ser mayor a 0");
            return null;
        }
        return valor;
    }

    private static ImportacionErrorNavDto Error(int fila, string error) => new() { Fila = fila, Error = error };

    // Comparación sin distinguir mayúsculas ni espacios sobrantes
    private static string Clave(string s) => string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    // Minúsculas, sin tildes y sin espacios sobrantes (para ubicar encabezados y leer Si/No)
    private static string Normalizar(string s)
    {
        var d = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in d)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return Clave(sb.ToString());
    }
}
