using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class TemporadaDto
{
    public int Id { get; set; }
    public int Anio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }
    public EstadoTemporada Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    public List<TemporadaAlmacenConteoDto> AlmacenesConConteo { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TemporadaAlmacenConteoDto
{
    public int AlmacenId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class CrearTemporadaDto
{
    public int Anio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public List<int>? AlmacenesConConteoIds { get; set; }
}

public class ActualizarTemporadaDto
{
    public int Id { get; set; }
    public int Anio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public List<int>? AlmacenesConConteoIds { get; set; }
}
