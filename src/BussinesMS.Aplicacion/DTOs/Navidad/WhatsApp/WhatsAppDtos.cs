namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class WhatsAppNumeroDto
{
    public bool Configurado { get; set; }
    public bool TieneNumero { get; set; }
    public string? Instancia { get; set; }
    /// <summary>Conectado | Desconectado | Conectando | SinConfigurar | Error | SinNumero</summary>
    public string Estado { get; set; } = "SinNumero";
    public string? NumeroEnmascarado { get; set; }
    /// <summary>Número vinculado completo (+591XXXXXXXX); null si aún no se conoce.</summary>
    public string? Numero { get; set; }
    public string? NombrePerfil { get; set; }
    public string? Detalle { get; set; }
    public DateTime? UltimoEstadoEn { get; set; }
}

public class WhatsAppQrDto
{
    public string Estado { get; set; } = string.Empty;
    public string? QrBase64 { get; set; }
    public string? Instancia { get; set; }
}

public class EnviarPruebaWhatsAppDto
{
    public string Telefono { get; set; } = string.Empty;
}

public class WhatsAppPruebaDto
{
    public bool Enviado { get; set; }
    public string DestinoEnmascarado { get; set; } = string.Empty;
}
