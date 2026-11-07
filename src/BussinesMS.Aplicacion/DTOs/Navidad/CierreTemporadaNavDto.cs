using BussinesMS.Dominio.Excepciones;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

// Cierre de temporada (2026-11-07). Lógica: docs/navidad/logica-cierre-temporada.md. Contratos: docs/navidad/contratos-cierre-temporada.md.
// Todos los números salen de ResumenNavCalculos (los mismos del dashboard y los reportes).

// Body opcional de POST Temporadas/{id}/cerrar: si viene, tiene que coincidir con el nombre de la temporada
public class CerrarTemporadaNavDto
{
    public string? ConfirmacionNombre { get; set; }
}

public class CierrePendienteVentaNavDto
{
    public int VentaId { get; set; }
    public int NroVenta { get; set; }
    public DateTime Fecha { get; set; }
    // "Credito" | "Reserva"
    public string Tipo { get; set; } = string.Empty;
    public decimal Saldo { get; set; }
}

// Una persona o un ítem del pendiente ("SANDRA 3.634")
public class CierrePendienteDetalleNavDto
{
    // Id de lo que se lista: almacenId, salidaId, vendedorId, clienteId, ventaId, proveedorId o aporteCapitalId según la clave (ver contratos)
    public int? Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal? Monto { get; set; }
    public int? Cantidad { get; set; }
    // Texto corto ya armado para mostrar
    public string Texto { get; set; } = string.Empty;
    // Solo creditosClientes: las ventas con saldo del cliente (para "Dar por incobrable")
    public List<CierrePendienteVentaNavDto>? Ventas { get; set; }
}

public class CierrePendienteNavDto
{
    // turnosAbiertos | cajasSinEntregar | salidasAbiertas | rendicionesPendientes | creditosClientes | reservasPendientes | deudaProveedores
    // | inversores | comisionesRuta | sueldosTienda | stockSobrante | mercaderiaPorRecibir
    public string Clave { get; set; } = string.Empty;
    // "bloquea" | "alerta"
    public string Nivel { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    // Bs (null si no aplica)
    public decimal? Monto { get; set; }
    // Cantidad de personas o ítems del detalle
    public int Cantidad { get; set; }
    // Unidades (solo stockSobrante y mercaderiaPorRecibir)
    public int? Unidades { get; set; }
    public List<CierrePendienteDetalleNavDto> Detalle { get; set; } = new();
}

public class CierreResultadoNavDto
{
    // Las líneas de resultado del dashboard + "stockSobranteAPerdida" (lo que se da de baja al cerrar; 0 si ya está cerrada)
    public List<DashboardResultadoLineaNavDto> Lineas { get; set; } = new();
    // Σ signo × monto (= resultado.ganancia del dashboard − stock sobrante a pérdida)
    public decimal Ganancia { get; set; }
    public List<string> Advertencias { get; set; } = new();
}

public class CierreDebemosNavDto
{
    public decimal Proveedores { get; set; }
    // Capital por devolver + comisión por pagar
    public decimal Inversores { get; set; }
    public decimal SueldosTienda { get; set; }
    public decimal Total { get; set; }
}

public class CierrePlataNavDto
{
    public decimal EfectivoNeto { get; set; }
    public decimal NosDeben { get; set; }
    public CierreDebemosNavDto Debemos { get; set; } = new();
    // EfectivoNeto + NosDeben − Debemos.Total
    public decimal TeQueda { get; set; }
}

public class CierreTemporadaResumenNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    public bool Cerrada { get; set; }
    // Abierta y sin bloqueantes
    public bool PuedeCerrar { get; set; }
    public int CantidadBloqueantes { get; set; }
    public int CantidadAlertas { get; set; }
    // Primero los que bloquean y después las alertas, en el orden de la tabla de la lógica. Solo vienen los que tienen algo
    public List<CierrePendienteNavDto> Pendientes { get; set; } = new();
    public CierreResultadoNavDto Resultado { get; set; } = new();
    public DashboardDevengadosNavDto Devengados { get; set; } = new();
    // Resultado.Ganancia − Devengados.Total
    public decimal GananciaNeta { get; set; }
    public CierrePlataNavDto Plata { get; set; } = new();
    // Capital propio aportado (informativo: no es deuda)
    public decimal CapitalPropio { get; set; }
    // Diferencia de la conciliación ("¿dónde está la ganancia?"), informativa
    public decimal DiferenciaConciliacion { get; set; }
    // Hora Bolivia del cálculo
    public DateTime CalculadoEn { get; set; }
}

// Foto guardada al cerrar (GET Temporadas/{id}/cierre)
public class CierreTemporadaFotoNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public DateTime FechaCierre { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public decimal Ganancia { get; set; }
    public decimal Devengados { get; set; }
    public decimal GananciaNeta { get; set; }
    public decimal DeudasIncobrables { get; set; }
    public int StockSobranteUnidades { get; set; }
    public decimal StockSobranteMonto { get; set; }
    public decimal EfectivoNeto { get; set; }
    public decimal NosDeben { get; set; }
    public decimal Debemos { get; set; }
    public decimal TeQueda { get; set; }
    public decimal CapitalPropio { get; set; }
    // El resumen completo tal como estaba al cerrar
    public CierreTemporadaResumenNavDto? Resumen { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CierreTemporadaResultadoNavDto
{
    public TemporadaDto Temporada { get; set; } = new();
    public CierreTemporadaFotoNavDto Cierre { get; set; } = new();
}

// 409 TEMPORADA_CON_PENDIENTES: el controller la devuelve con la lista en data
public class TemporadaConPendientesException : ExcepcionDominio
{
    public List<CierrePendienteNavDto> Pendientes { get; }

    public TemporadaConPendientesException(List<CierrePendienteNavDto> pendientes)
        : base($"No se puede cerrar la temporada: hay {pendientes.Count(p => p.Nivel == "bloquea")} pendiente(s) que bloquean el cierre", 409, "TEMPORADA_CON_PENDIENTES")
    {
        Pendientes = pendientes;
    }
}
