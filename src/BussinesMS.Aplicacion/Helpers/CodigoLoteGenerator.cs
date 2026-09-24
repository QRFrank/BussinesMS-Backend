namespace BussinesMS.Aplicacion.Helpers;

public static class CodigoLoteGenerator
{
    public static string Generar(string nombreProducto, string presentacion, DateTime fechaCreacion)
    {
        var abreviaturaProducto = GenerarAbreviaturaProducto(nombreProducto);
        var abreviaturaPresentacion = GenerarAbreviaturaPresentacion(presentacion);
        var fecha = fechaCreacion.ToString("ddMMyy");
        var hora = fechaCreacion.ToString("HHmm");

        return $"{abreviaturaProducto}{abreviaturaPresentacion}-{fecha}-{hora}";
    }

    private static string GenerarAbreviaturaProducto(string nombre)
    {
        var palabras = nombre
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length > 2)
            .Take(2)
            .Select(p => p.Length >= 3 ? p[..3].ToUpper() : p.ToUpper());

        return string.Concat(palabras);
    }

    private static string GenerarAbreviaturaPresentacion(string presentacion)
    {
        return presentacion
            .Replace(" ", "")
            .Replace(".", "")
            .ToUpper();
    }
}
