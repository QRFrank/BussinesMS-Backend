namespace BussinesMS.Aplicacion.DTOs.Navidad;

// Deuda dada por incobrable (cierre de temporada)
public class IncobrableNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public int? VentaId { get; set; }
    public int? NroVenta { get; set; }
    public int? ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public int? VendedorId { get; set; }
    public string? VendedorNombre { get; set; }
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public bool Anulado { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? UsuarioAnulacionId { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<IncobrableNavAplicacionDto> Aplicaciones { get; set; } = new();
}

public class IncobrableNavAplicacionDto
{
    public int VentaId { get; set; }
    public int NroVenta { get; set; }
    public decimal Monto { get; set; }
}

// Cliente: todo el saldo de una venta de tienda a crédito (o reserva entregada)
public class RegistrarIncobrableClienteNavDto
{
    public int VentaId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

// Vendedor de ruta: parte (o todo) del saldo por rendir, aplicado de la venta más antigua a la más nueva
public class RegistrarIncobrableVendedorRutaNavDto
{
    public int VendedorId { get; set; }
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
