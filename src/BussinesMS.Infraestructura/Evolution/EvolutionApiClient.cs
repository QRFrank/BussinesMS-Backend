using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Excepciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BussinesMS.Infraestructura.Evolution;

/// <summary>
/// Cliente tipado de Evolution API v2. La apikey viaja solo en el header "apikey" (nunca en URL ni logs).
/// No se loguean textos ni números completos.
/// </summary>
public class EvolutionApiClient : IEvolutionApiClient
{
    private readonly HttpClient _http;
    private readonly IOptions<WhatsAppOpciones> _opciones;
    private readonly ILogger<EvolutionApiClient> _logger;

    public EvolutionApiClient(HttpClient http, IOptions<WhatsAppOpciones> opciones, ILogger<EvolutionApiClient> logger)
    {
        _http = http;
        _opciones = opciones;
        _logger = logger;
    }

    private WhatsAppOpciones Cfg => _opciones.Value;

    private void AsegurarConfigurado()
    {
        if (!Cfg.EstaConfigurado)
            throw new ExcepcionDominio("Falta configurar Evolution en el servidor", 409, "WHATSAPP_NO_CONFIGURADO");
    }

    private static ExcepcionDominio NoDisponible() =>
        new("No se pudo contactar a Evolution API (¿Docker levantado?)", 503, "EVOLUTION_NO_DISPONIBLE");

    private static ExcepcionDominio ApiKeyInvalida() =>
        new("Evolution rechazó la API key configurada en el servidor", 502, "EVOLUTION_APIKEY_INVALIDA");

    /// <summary>Envía la request con la apikey. Traduce errores de red/timeout a ExcepcionDominio.</summary>
    private async Task<(HttpStatusCode Status, string Cuerpo)> EnviarAsync(
        HttpMethod metodo, string ruta, object? body, TimeSpan? timeout, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(metodo, ruta);
        req.Headers.TryAddWithoutValidation("apikey", Cfg.Evolution.ApiKey);
        if (body != null) req.Content = JsonContent.Create(body);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (timeout.HasValue) cts.CancelAfter(timeout.Value);

        try
        {
            using var resp = await _http.SendAsync(req, cts.Token);
            var cuerpo = await resp.Content.ReadAsStringAsync(cts.Token);
            return (resp.StatusCode, cuerpo);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            if (ct.IsCancellationRequested) throw;
            _logger.LogWarning("Evolution no respondió ({Metodo} {Ruta}): {Tipo}", metodo.Method, ruta, ex.GetType().Name);
            throw NoDisponible();
        }
    }

    /// <summary>Lanza ExcepcionDominio para 401/403/5xx. Deja pasar el resto para que lo interprete el llamador.</summary>
    private static void ValidarErroresGenerales(HttpStatusCode status)
    {
        if (status == HttpStatusCode.Unauthorized || status == HttpStatusCode.Forbidden) throw ApiKeyInvalida();
        if ((int)status >= 500) throw NoDisponible();
    }

    public async Task<EstadoInstanciaEvolution> ObtenerEstadoAsync(string instancia, CancellationToken ct = default)
    {
        if (!Cfg.EstaConfigurado)
            return new EstadoInstanciaEvolution { Estado = EstadoWhatsApp.SinConfigurar, Detalle = "Falta configurar Evolution en el servidor" };

        try
        {
            var timeout = TimeSpan.FromSeconds(Math.Max(1, Cfg.Evolution.TimeoutEstadoSegundos));
            var (status, cuerpo) = await EnviarAsync(HttpMethod.Get,
                $"instance/connectionState/{Uri.EscapeDataString(instancia)}", null, timeout, ct);

            if (status == HttpStatusCode.NotFound)
                return new EstadoInstanciaEvolution { Estado = EstadoWhatsApp.NoExiste };
            ValidarErroresGenerales(status);
            if (!((int)status is >= 200 and < 300))
                return new EstadoInstanciaEvolution { Estado = EstadoWhatsApp.Error, Detalle = $"Evolution respondió {(int)status}" };

            var estado = MapearEstado(LeerEstado(cuerpo));
            var resultado = new EstadoInstanciaEvolution { Estado = estado };

            if (estado == EstadoWhatsApp.Conectado)
                await CompletarDatosPerfilAsync(instancia, resultado, timeout, ct);

            return resultado;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var detalle = ex is ExcepcionDominio ed ? ed.Message : "Error consultando el estado de WhatsApp";
            if (ex is not ExcepcionDominio)
                _logger.LogWarning("Error consultando estado de Evolution: {Tipo}", ex.GetType().Name);
            return new EstadoInstanciaEvolution { Estado = EstadoWhatsApp.Error, Detalle = detalle };
        }
    }

    public async Task CrearInstanciaAsync(string instancia, CancellationToken ct = default)
    {
        AsegurarConfigurado();
        var body = new { instanceName = instancia, integration = "WHATSAPP-BAILEYS", qrcode = true };
        var (status, cuerpo) = await EnviarAsync(HttpMethod.Post, "instance/create", body, TimeSpan.FromSeconds(Cfg.Evolution.TimeoutSegundos), ct);

        if (status == HttpStatusCode.Forbidden && cuerpo.Contains("already in use", StringComparison.OrdinalIgnoreCase))
            return; // ya existía

        ValidarErroresGenerales(status);
        if (!((int)status is >= 200 and < 300))
            throw new ExcepcionDominio($"Evolution no pudo crear la instancia (HTTP {(int)status})", 502, "EVOLUTION_ERROR");
    }

    public async Task<QrInstanciaEvolution> ObtenerQrAsync(string instancia, CancellationToken ct = default)
    {
        AsegurarConfigurado();
        var timeout = TimeSpan.FromSeconds(Cfg.Evolution.TimeoutSegundos);
        var (status, cuerpo) = await EnviarAsync(HttpMethod.Get, $"instance/connect/{Uri.EscapeDataString(instancia)}", null, timeout, ct);

        ValidarErroresGenerales(status);
        if (status == HttpStatusCode.NotFound)
            throw new ExcepcionDominio("La instancia de WhatsApp no existe en Evolution", 404, "INSTANCIA_NO_EXISTE");
        if (!((int)status is >= 200 and < 300))
            throw new ExcepcionDominio($"Evolution no pudo generar el QR (HTTP {(int)status})", 502, "EVOLUTION_ERROR");

        string? base64 = null;
        string? estadoTexto = null;
        try
        {
            using var doc = JsonDocument.Parse(cuerpo);
            var raiz = doc.RootElement;
            if (raiz.ValueKind == JsonValueKind.Object)
            {
                base64 = TextoONull(raiz, "base64");
                if (raiz.TryGetProperty("instance", out var inst) && inst.ValueKind == JsonValueKind.Object)
                    estadoTexto = TextoONull(inst, "state");
            }
        }
        catch (JsonException)
        {
            // cuerpo no JSON: se trata como QR todavía no disponible
        }

        if (MapearEstado(estadoTexto) == EstadoWhatsApp.Conectado)
            return new QrInstanciaEvolution { Estado = EstadoWhatsApp.Conectado };

        if (!string.IsNullOrWhiteSpace(base64) && !base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            base64 = "data:image/png;base64," + base64;

        return new QrInstanciaEvolution
        {
            QrBase64 = string.IsNullOrWhiteSpace(base64) ? null : base64,
            Estado = EstadoWhatsApp.Conectando
        };
    }

    public async Task DesconectarAsync(string instancia, CancellationToken ct = default)
    {
        AsegurarConfigurado();
        var (status, cuerpo) = await EnviarAsync(HttpMethod.Delete, $"instance/logout/{Uri.EscapeDataString(instancia)}",
            null, TimeSpan.FromSeconds(Cfg.Evolution.TimeoutSegundos), ct);

        if (status == HttpStatusCode.NotFound) return;
        if (status == HttpStatusCode.BadRequest && cuerpo.Contains("not connected", StringComparison.OrdinalIgnoreCase)) return;
        ValidarErroresGenerales(status);
        if (!((int)status is >= 200 and < 300))
            throw new ExcepcionDominio($"Evolution no pudo desconectar WhatsApp (HTTP {(int)status})", 502, "EVOLUTION_ERROR");
    }

    public async Task BorrarInstanciaAsync(string instancia, CancellationToken ct = default)
    {
        AsegurarConfigurado();
        var (status, _) = await EnviarAsync(HttpMethod.Delete, $"instance/delete/{Uri.EscapeDataString(instancia)}",
            null, TimeSpan.FromSeconds(Cfg.Evolution.TimeoutSegundos), ct);

        if (status == HttpStatusCode.NotFound) return;
        ValidarErroresGenerales(status);
        if (!((int)status is >= 200 and < 300))
            throw new ExcepcionDominio($"Evolution no pudo borrar la instancia (HTTP {(int)status})", 502, "EVOLUTION_ERROR");
    }

    public async Task EnviarTextoAsync(string instancia, string numeroNormalizado, string texto, CancellationToken ct = default)
    {
        AsegurarConfigurado();
        var body = new { number = numeroNormalizado.TrimStart('+'), text = texto };
        var (status, _) = await EnviarAsync(HttpMethod.Post, $"message/sendText/{Uri.EscapeDataString(instancia)}",
            body, TimeSpan.FromSeconds(Cfg.Evolution.TimeoutSegundos), ct);

        ValidarErroresGenerales(status);
        if (!((int)status is >= 200 and < 300))
        {
            _logger.LogWarning("Evolution rechazó un envío a {Destino}: HTTP {Status}",
                EnmascaradorTelefono.Enmascarar(numeroNormalizado), (int)status);
            throw new ExcepcionDominio($"Evolution no pudo enviar el mensaje (HTTP {(int)status})", 502, "EVOLUTION_ERROR");
        }
    }

    public async Task EnviarDocumentoAsync(string instancia, string numeroNormalizado, byte[] contenido, string mimetype,
        string nombreArchivo, string? caption, CancellationToken ct = default)
    {
        AsegurarConfigurado();
        // Evolution v2: cuerpo plano; media = base64 puro (sin prefijo data:)
        var body = new
        {
            number = numeroNormalizado.TrimStart('+'),
            mediatype = "document",
            mimetype,
            caption = caption ?? string.Empty,
            media = Convert.ToBase64String(contenido),
            fileName = nombreArchivo
        };
        var (status, _) = await EnviarAsync(HttpMethod.Post, $"message/sendMedia/{Uri.EscapeDataString(instancia)}",
            body, TimeSpan.FromSeconds(Math.Max(1, Cfg.Evolution.TimeoutSegundos)), ct);

        ValidarErroresGenerales(status);
        if (!((int)status is >= 200 and < 300))
        {
            _logger.LogWarning("Evolution rechazó un envío de documento a {Destino}: HTTP {Status}",
                EnmascaradorTelefono.Enmascarar(numeroNormalizado), (int)status);
            throw new ExcepcionDominio($"Evolution no pudo enviar el documento (HTTP {(int)status})", 502, "EVOLUTION_ERROR");
        }
    }

    // ---------- helpers ----------

    private static string? LeerEstado(string cuerpo)
    {
        try
        {
            using var doc = JsonDocument.Parse(cuerpo);
            var raiz = doc.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object) return null;
            if (raiz.TryGetProperty("instance", out var inst) && inst.ValueKind == JsonValueKind.Object)
                return TextoONull(inst, "state");
            return TextoONull(raiz, "state");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static EstadoWhatsApp MapearEstado(string? state) => state?.ToLowerInvariant() switch
    {
        "open" => EstadoWhatsApp.Conectado,
        "connecting" => EstadoWhatsApp.Conectando,
        "close" or "closed" => EstadoWhatsApp.Desconectado,
        _ => EstadoWhatsApp.Desconectado
    };

    /// <summary>Lee número/perfil de fetchInstances (formatos v2.0/2.1 y v2.2+). Si falla, se ignora.</summary>
    private async Task CompletarDatosPerfilAsync(string instancia, EstadoInstanciaEvolution resultado, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            var (status, cuerpo) = await EnviarAsync(HttpMethod.Get,
                $"instance/fetchInstances?instanceName={Uri.EscapeDataString(instancia)}", null, timeout, ct);
            if (!((int)status is >= 200 and < 300)) return;

            using var doc = JsonDocument.Parse(cuerpo);
            var raiz = doc.RootElement;
            var elementos = raiz.ValueKind == JsonValueKind.Array ? raiz.EnumerateArray().ToList() : new List<JsonElement> { raiz };

            foreach (var el in elementos)
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var nodo = el.TryGetProperty("instance", out var inst) && inst.ValueKind == JsonValueKind.Object ? inst : el;

                var nombre = TextoONull(nodo, "instanceName") ?? TextoONull(nodo, "name");
                if (nombre != null && !string.Equals(nombre, instancia, StringComparison.OrdinalIgnoreCase)) continue;

                resultado.NumeroJid = TextoONull(nodo, "ownerJid") ?? TextoONull(nodo, "owner") ?? TextoONull(nodo, "number");
                resultado.NombrePerfil = TextoONull(nodo, "profileName");
                return;
            }
        }
        catch (Exception ex) when (ex is JsonException or ExcepcionDominio)
        {
            // opcional: se ignora
        }
    }

    private static string? TextoONull(JsonElement el, string propiedad)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        if (!el.TryGetProperty(propiedad, out var v) || v.ValueKind != JsonValueKind.String) return null;
        var s = v.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
