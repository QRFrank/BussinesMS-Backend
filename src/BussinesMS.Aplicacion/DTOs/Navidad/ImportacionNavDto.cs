namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class ImportacionErrorNavDto
{
    public int Fila { get; set; }
    public string Error { get; set; } = string.Empty;
}

public class ImportacionOmitidoNavDto
{
    public int Fila { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public class ImportacionProductosResultadoNavDto
{
    public int ProveedoresCreados { get; set; }
    public int CategoriasCreadas { get; set; }
    public int ProductosCreados { get; set; }
    public List<ImportacionOmitidoNavDto> Omitidos { get; set; } = new();
    // Solo se llena si hubo errores (en ese caso no se guardó nada)
    public List<ImportacionErrorNavDto> Errores { get; set; } = new();
}
