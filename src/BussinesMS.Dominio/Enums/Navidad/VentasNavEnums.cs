namespace BussinesMS.Dominio.Enums.Navidad;

// Origen de la venta: Tienda (módulo 6) o Ruta (módulo 7)
public enum CanalVentaNav
{
    Tienda = 1,
    Ruta = 2
}

// Directa: se entrega en el momento. Reserva: se entrega después (total o de a poco)
public enum TipoVentaNav
{
    Directa = 1,
    Reserva = 2
}

// Fija desde que se crea la venta
public enum CondicionPagoVentaNav
{
    Contado = 1,
    Credito = 2
}

public enum EstadoPagoVentaNav
{
    Pendiente = 1,
    Parcial = 2,
    Pagado = 3
}

public enum EstadoEntregaVentaNav
{
    Pendiente = 1,
    Parcial = 2,
    Entregado = 3
}

public enum MotivoConsumoNav
{
    Propio = 1,
    Invitacion = 2,
    Otro = 3
}

// Motivo del faltante (diferencia negativa) en un conteo de inventario
public enum MotivoDiferenciaConteo
{
    VentaNoRegistrada = 1,
    Consumo = 2,
    MermaRobo = 3,
    ErrorConteo = 4
}
