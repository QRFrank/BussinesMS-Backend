using BussinesMS.Aplicacion.Seguridad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

// No paginado
public class StockNavFiltroDto
{
    public int? TemporadaId { get; set; }
    public int? AlmacenId { get; set; }
    public int? ProductoId { get; set; }
    public int? ProveedorId { get; set; }
    public bool IncluirSinStock { get; set; } = false;
}

// Una fila por producto × almacén (Ajuste 2: sin lotes)
public class StockNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    // Proveedor principal del producto
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    // Color de la tarjeta del producto (hex #RRGGBB o null)
    public string? Color { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int Stock { get; set; }
    // Unidades apartadas por reservas con bloqueo en este almacén (0 si no hay)
    public int Bloqueado { get; set; }
    // Libre para vender o trasladar: Stock − Bloqueado (mínimo 0)
    public int Disponible { get; set; }
    // Costo promedio ponderado del producto en la temporada (redondeado a 2; 0 si no tuvo entradas)
    [OcultoSinPrecioCompra] public decimal? CostoPromedio { get; set; }
    // Stock de esta fila × costo promedio (sin redondear el costo), redondeado a 2
    [OcultoSinPrecioCompra] public decimal? Valorizado { get; set; }
}
