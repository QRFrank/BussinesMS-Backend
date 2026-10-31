using System.Text.RegularExpressions;

namespace BussinesMS.Aplicacion.Comun;

/// <summary>Normaliza teléfonos a formato internacional "+591XXXXXXXX".</summary>
public static partial class NormalizadorTelefono
{
    private const string CodigoPais = "591";

    [GeneratedRegex(@"^\+\d{8,15}$")]
    private static partial Regex PatronValido();

    public static bool TryNormalizar(string? telefono, out string normalizado)
    {
        normalizado = string.Empty;
        if (string.IsNullOrWhiteSpace(telefono)) return false;

        var limpio = new string(telefono.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '.' && c != '(' && c != ')').ToArray());
        if (limpio.Length == 0) return false;

        if (!limpio.StartsWith('+'))
        {
            var soloDigitos = limpio.All(char.IsDigit);
            if (soloDigitos && limpio.StartsWith(CodigoPais) && limpio.Length == CodigoPais.Length + 8)
                limpio = "+" + limpio;
            else
                limpio = "+" + CodigoPais + limpio;
        }

        if (!PatronValido().IsMatch(limpio)) return false;
        normalizado = limpio;
        return true;
    }

    /// <summary>Devuelve el teléfono normalizado o null si no es válido.</summary>
    public static string? Normalizar(string? telefono) =>
        TryNormalizar(telefono, out var n) ? n : null;

    public static bool EsValido(string? telefono) => TryNormalizar(telefono, out _);
}
