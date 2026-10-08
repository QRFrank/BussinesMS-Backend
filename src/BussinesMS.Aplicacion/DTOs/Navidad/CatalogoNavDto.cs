namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class CopiarCatalogoDto
{
    public int TemporadaOrigenId { get; set; }
}

public class CopiaCatalogoResultadoDto
{
    public int TemporadaOrigenId { get; set; }
    public int TemporadaDestinoId { get; set; }
    public int Proveedores { get; set; }
    public int CodigosCliente { get; set; }
    public int Productos { get; set; }
    public int Presentaciones { get; set; }
    public int Vendedores { get; set; }
    public int VendedoresOmitidos { get; set; }
}
