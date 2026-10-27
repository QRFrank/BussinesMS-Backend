namespace BussinesMS.Aplicacion.DTOs.Navidad;

// ---------- Crear / editar reservas (api/Navidad/Reservas) ----------

public class ReservaBloqueoItemDto
{
    // Almacén navideño activo
    public int AlmacenId { get; set; }
    public int Unidades { get; set; }
}

public class ReservaLineaDto : VentaLineaDto
{
    // Crear: bloqueos de la línea (null o vacío = sin bloqueo).
    // Editar: estado deseado de los bloqueos de la línea (null = dejar los actuales; [] = quitarlos todos, solo admin)
    public List<ReservaBloqueoItemDto>? Bloqueos { get; set; }
}

public class CrearReservaNavDto
{
    // Si el usuario logueado es vendedor activo de la temporada se fuerza ese (salvo el admin, que puede elegir); si no, obligatorio
    public int? VendedorId { get; set; }
    // Siempre obligatorio
    public int? ClienteId { get; set; }
    // 1 Contado | 2 Crédito
    public int CondicionPago { get; set; }
    public decimal Descuento { get; set; }
    public string? Observacion { get; set; }
    // Contado: obligatorio (monto = neto o sin monto); Crédito: adelanto opcional (> 0 y < neto)
    public CobroInicialVentaDto? Cobro { get; set; }
    // Opcional: tienda de la sesión donde se recibe el cobro inicial (caja de tienda). Debe ser una tienda navideña activa. No se guarda en la reserva
    public int? AlmacenId { get; set; }
    public List<ReservaLineaDto> Detalles { get; set; } = new();
}

// PUT: el conjunto de líneas reemplaza al actual (las que faltan quedan en 0). No se cambian condición ni vendedor
public class EditarReservaNavDto
{
    public int? ClienteId { get; set; }
    public decimal Descuento { get; set; }
    public string? Observacion { get; set; }
    public List<ReservaLineaDto> Detalles { get; set; } = new();
}

public class LiberarBloqueoReservaDto
{
    public int BloqueoId { get; set; }
    // null = liberar todo el bloqueo; si viene, 1..unidades del bloqueo
    public int? Unidades { get; set; }
}

// ---------- Productos para armar la reserva ----------

public class ReservaProductoNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public decimal PrecioCatalogo { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    // Σ stock de los almacenes navideños activos
    public int StockTotal { get; set; }
    // Lo pedido y todavía no recibido (misma fuente que Faltantes)
    public int PorLlegar { get; set; }
    // PorLlegar − Σ partes sin bloqueo de las reservas vigentes (puede ser negativo)
    public int PorLlegarLibre { get; set; }
    public List<ReservaProductoAlmacenNavDto> Almacenes { get; set; } = new();
}

public class ReservaProductoAlmacenNavDto
{
    public int AlmacenId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int Bloqueado { get; set; }
    // max(0, Stock − Bloqueado)
    public int Libre { get; set; }
}

// ---------- Cobertura de reservas ----------

public class ReservaCoberturaNavFiltroDto
{
    public int? TemporadaId { get; set; }
}

public class ReservaCoberturaNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    // Σ pendiente de entrega de reservas no anuladas
    public int ReservadoPendiente { get; set; }
    // Parte del reservado pendiente que está bloqueada (Σ bloqueos vigentes)
    public int Bloqueado { get; set; }
    // ReservadoPendiente − Bloqueado: la parte que debe cubrir lo que falta llegar
    public int SinBloqueo { get; set; }
    // Σ stock de todos los almacenes navideños
    public int StockTotal { get; set; }
    // Pedido y todavía no recibido (misma lógica que Faltantes: Σ de las filas con faltante > 0)
    public int PorLlegar { get; set; }
    // PorLlegar − SinBloqueo (puede ser negativo)
    public int PorLlegarLibre { get; set; }
    // max(0, −PorLlegarLibre): unidades reservadas sin bloqueo que ningún pedido cubre
    public int SinCobertura { get; set; }
    // Bloqueos vigentes por almacén
    public List<ReservaCoberturaAlmacenNavDto> Bloqueos { get; set; } = new();
}

public class ReservaCoberturaAlmacenNavDto
{
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int Unidades { get; set; }
}
