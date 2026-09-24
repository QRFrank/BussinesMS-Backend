namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class CrearTrasladoPorLoteDto
{
    public int LoteId { get; set; }
    public int AlmacenOrigenId { get; set; }
    public int AlmacenDestinoId { get; set; }
    public int CantidadUnidades { get; set; }
    public string? Observacion { get; set; }
}
