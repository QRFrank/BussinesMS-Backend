namespace BussinesMS.Aplicacion.Comun;

/// <summary>Enmascara teléfonos/jids de WhatsApp para mostrar y loguear (oculta los últimos 4 dígitos).</summary>
public static class EnmascaradorTelefono
{
    /// <summary>"59171234567@s.whatsapp.net" -> "+59171234567" (completo, sin enmascarar).</summary>
    public static string Completo(string? telefonoOJid)
    {
        if (string.IsNullOrWhiteSpace(telefonoOJid)) return string.Empty;
        var digitos = new string(telefonoOJid.Split('@')[0].Split(':')[0].Where(char.IsDigit).ToArray());
        return digitos.Length == 0 ? string.Empty : "+" + digitos;
    }

    /// <summary>"+59171234567" o "59171234567@s.whatsapp.net" -> "+591 7123****".</summary>
    public static string Enmascarar(string? telefonoOJid)
    {
        if (string.IsNullOrWhiteSpace(telefonoOJid)) return string.Empty;

        var parte = telefonoOJid.Split('@')[0].Split(':')[0];
        var digitos = new string(parte.Where(char.IsDigit).ToArray());
        if (digitos.Length == 0) return string.Empty;
        if (digitos.Length <= 4) return "****";

        if (digitos.StartsWith("591") && digitos.Length > 7)
            return $"+591 {digitos[3..^4]}****";

        return $"+{digitos[..^4]}****";
    }
}
