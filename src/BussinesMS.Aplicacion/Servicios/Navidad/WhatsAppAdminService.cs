using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Dominio.Excepciones;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

public class WhatsAppAdminService : IWhatsAppAdminService
{
    private const string TextoPrueba = "Mensaje de prueba de BussinesMS. Si lo recibiste, WhatsApp está funcionando.";

    private readonly IWhatsAppInstanciaRepository _repositorio;
    private readonly IEvolutionApiClient _evolution;
    private readonly IOptions<WhatsAppOpciones> _opciones;
    private readonly ILogger<WhatsAppAdminService> _logger;

    public WhatsAppAdminService(
        IWhatsAppInstanciaRepository repositorio,
        IEvolutionApiClient evolution,
        IOptions<WhatsAppOpciones> opciones,
        ILogger<WhatsAppAdminService> logger)
    {
        _repositorio = repositorio;
        _evolution = evolution;
        _opciones = opciones;
        _logger = logger;
    }

    private bool Configurado => _opciones.Value.EstaConfigurado;

    private void ExigirConfigurado()
    {
        if (!Configurado)
            throw new ExcepcionDominio("Falta configurar Evolution en el servidor", 409, "WHATSAPP_NO_CONFIGURADO");
    }

    private static string NuevoNombreInstancia() => $"bussinesms-{DateTime.UtcNow:yyyyMMddHHmmss}";

    private static string NombreEstado(EstadoWhatsApp estado) => estado switch
    {
        EstadoWhatsApp.Conectado => "Conectado",
        EstadoWhatsApp.Conectando => "Conectando",
        EstadoWhatsApp.SinConfigurar => "SinConfigurar",
        EstadoWhatsApp.Error => "Error",
        _ => "Desconectado" // Desconectado y NoExiste
    };

    // ---------------- Obtener número ----------------

    public async Task<WhatsAppNumeroDto> ObtenerNumeroAsync()
    {
        var fila = await _repositorio.ObtenerVigenteAsync();

        if (!Configurado)
        {
            return new WhatsAppNumeroDto
            {
                Configurado = false,
                TieneNumero = fila != null,
                Instancia = fila?.NombreInstancia,
                Estado = "SinConfigurar",
                NumeroEnmascarado = fila?.NumeroEnmascarado,
                Numero = fila?.Numero,
                NombrePerfil = fila?.NombrePerfil,
                UltimoEstadoEn = fila?.UltimoEstadoEn
            };
        }

        if (fila == null)
            return new WhatsAppNumeroDto { Configurado = true, TieneNumero = false, Estado = "SinNumero" };

        var estado = await _evolution.ObtenerEstadoAsync(fila.NombreInstancia);
        return await ConstruirDtoYPersistirAsync(fila, estado);
    }

    private async Task<WhatsAppNumeroDto> ConstruirDtoYPersistirAsync(WhatsAppInstancia fila, EstadoInstanciaEvolution estado)
    {
        var nombreEstado = NombreEstado(estado.Estado);

        if (estado.Estado != EstadoWhatsApp.Error && estado.Estado != EstadoWhatsApp.SinConfigurar)
            await PersistirEstadoAsync(fila, estado, nombreEstado);

        return new WhatsAppNumeroDto
        {
            Configurado = true,
            TieneNumero = true,
            Instancia = fila.NombreInstancia,
            Estado = nombreEstado,
            NumeroEnmascarado = fila.NumeroEnmascarado,
            Numero = fila.Numero,
            NombrePerfil = fila.NombrePerfil,
            Detalle = estado.Detalle,
            UltimoEstadoEn = fila.UltimoEstadoEn
        };
    }

    private async Task PersistirEstadoAsync(WhatsAppInstancia fila, EstadoInstanciaEvolution estado, string nombreEstado)
    {
        var cambio = false;

        if (fila.UltimoEstado != nombreEstado) { fila.UltimoEstado = nombreEstado; cambio = true; }

        if (estado.Estado == EstadoWhatsApp.Conectado)
        {
            var enmascarado = EnmascaradorTelefono.Enmascarar(estado.NumeroJid);
            if (!string.IsNullOrEmpty(enmascarado) && fila.NumeroEnmascarado != enmascarado)
            {
                fila.NumeroEnmascarado = enmascarado;
                cambio = true;
            }
            var completo = EnmascaradorTelefono.Completo(estado.NumeroJid);
            if (!string.IsNullOrEmpty(completo) && fila.Numero != completo)
            {
                fila.Numero = completo;
                cambio = true;
            }
            if (!string.IsNullOrWhiteSpace(estado.NombrePerfil) && fila.NombrePerfil != estado.NombrePerfil)
            {
                fila.NombrePerfil = Truncar(estado.NombrePerfil, 100);
                cambio = true;
            }
        }

        if (!cambio) return;
        fila.UltimoEstadoEn = DateTime.UtcNow;
        await _repositorio.ActualizarAsync(fila);
    }

    private static string Truncar(string valor, int max) => valor.Length <= max ? valor : valor[..max];

    // ---------------- QR ----------------

    public async Task<WhatsAppQrDto> ObtenerQrAsync()
    {
        ExigirConfigurado();

        var fila = await _repositorio.ObtenerVigenteAsync();
        string nombre;
        if (fila == null)
        {
            nombre = NuevoNombreInstancia();
            await _evolution.CrearInstanciaAsync(nombre);
            fila = await CrearFilaAsync(nombre);
        }
        else
        {
            nombre = fila.NombreInstancia;
            await _evolution.CrearInstanciaAsync(nombre); // tolera "ya existe"
        }

        return await PedirQrAsync(fila);
    }

    private async Task<WhatsAppQrDto> PedirQrAsync(WhatsAppInstancia fila)
    {
        var qr = await _evolution.ObtenerQrAsync(fila.NombreInstancia);

        if (qr.Estado == EstadoWhatsApp.Conectado)
        {
            var estado = await _evolution.ObtenerEstadoAsync(fila.NombreInstancia);
            if (estado.Estado == EstadoWhatsApp.Conectado)
                await PersistirEstadoAsync(fila, estado, "Conectado");
            return new WhatsAppQrDto { Estado = "Conectado", Instancia = fila.NombreInstancia };
        }

        return new WhatsAppQrDto { Estado = "Conectando", QrBase64 = qr.QrBase64, Instancia = fila.NombreInstancia };
    }

    private async Task<WhatsAppInstancia> CrearFilaAsync(string nombre)
    {
        try
        {
            return await _repositorio.CrearAsync(new WhatsAppInstancia
            {
                NombreInstancia = nombre,
                UltimoEstado = "Conectando",
                UltimoEstadoEn = DateTime.UtcNow
            });
        }
        catch
        {
            // La fila no se guardó: no dejar la instancia huérfana en Evolution
            await BorrarEnEvolutionBestEffortAsync(nombre);
            throw;
        }
    }

    // ---------------- Desconectar ----------------

    public async Task<WhatsAppNumeroDto> DesconectarAsync()
    {
        ExigirConfigurado();

        var fila = await _repositorio.ObtenerVigenteAsync()
            ?? throw new ExcepcionDominio("No hay un número vinculado", 404, "SIN_NUMERO");

        await _evolution.DesconectarAsync(fila.NombreInstancia);
        _logger.LogInformation("WhatsApp desconectado (instancia {Instancia})", fila.NombreInstancia);

        var estado = await _evolution.ObtenerEstadoAsync(fila.NombreInstancia);
        return await ConstruirDtoYPersistirAsync(fila, estado);
    }

    // ---------------- Cambiar número ----------------

    public async Task<WhatsAppQrDto> CambiarNumeroAsync()
    {
        ExigirConfigurado();

        var vieja = await _repositorio.ObtenerVigenteAsync();

        // 1) Borrar la instancia vieja en Evolution. Si falla, se lanza el error y la BD no se toca.
        if (vieja != null)
        {
            try { await _evolution.DesconectarAsync(vieja.NombreInstancia); }
            catch (ExcepcionDominio ex) { _logger.LogWarning("Logout best-effort falló: {Codigo}", ex.CodigoError); }

            await _evolution.BorrarInstanciaAsync(vieja.NombreInstancia);
        }

        // 2) Crear la instancia nueva en Evolution (si falla no se toca la BD: la vieja queda vigente)
        var nombreNuevo = NuevoNombreInstancia();
        if (vieja != null && string.Equals(vieja.NombreInstancia, nombreNuevo, StringComparison.Ordinal))
            nombreNuevo += "b";
        await _evolution.CrearInstanciaAsync(nombreNuevo);

        // 3) BD: baja lógica de la vieja y alta de la nueva (el índice único es filtrado por IsActive)
        if (vieja != null)
            await _repositorio.EliminarAsync(vieja.Id);

        WhatsAppInstancia nueva;
        try
        {
            nueva = await _repositorio.CrearAsync(new WhatsAppInstancia
            {
                NombreInstancia = nombreNuevo,
                UltimoEstado = "Conectando",
                UltimoEstadoEn = DateTime.UtcNow
            });
        }
        catch
        {
            if (vieja != null)
            {
                // revertir la baja lógica para no dejar al sistema sin instancia vigente
                vieja.IsActive = true;
                vieja.DeletedAt = null;
                vieja.DeletedByUsuarioId = null;
                await _repositorio.ActualizarAsync(vieja);
            }
            await BorrarEnEvolutionBestEffortAsync(nombreNuevo);
            throw;
        }

        _logger.LogInformation("WhatsApp: número cambiado, instancia nueva {Instancia}", nombreNuevo);
        return await PedirQrAsync(nueva);
    }

    private async Task BorrarEnEvolutionBestEffortAsync(string nombre)
    {
        try { await _evolution.BorrarInstanciaAsync(nombre); }
        catch (Exception ex) { _logger.LogWarning("No se pudo limpiar la instancia {Instancia}: {Tipo}", nombre, ex.GetType().Name); }
    }

    // ---------------- Prueba ----------------

    public async Task<WhatsAppPruebaDto> EnviarPruebaAsync(EnviarPruebaWhatsAppDto dto)
    {
        if (!NormalizadorTelefono.TryNormalizar(dto?.Telefono, out var destino))
            throw new ExcepcionDominio("El teléfono no es válido", 400, "TELEFONO_INVALIDO");

        ExigirConfigurado();

        var fila = await _repositorio.ObtenerVigenteAsync()
            ?? throw new ExcepcionDominio("No hay un número vinculado", 404, "SIN_NUMERO");

        var estado = await _evolution.ObtenerEstadoAsync(fila.NombreInstancia);
        if (estado.Estado != EstadoWhatsApp.Conectado)
            throw new ExcepcionDominio("El número de WhatsApp no está conectado", 409, "WHATSAPP_DESCONECTADO");

        await _evolution.EnviarTextoAsync(fila.NombreInstancia, destino, TextoPrueba);

        var enmascarado = EnmascaradorTelefono.Enmascarar(destino);
        _logger.LogInformation("Mensaje de prueba de WhatsApp enviado a {Destino}", enmascarado);
        return new WhatsAppPruebaDto { Enviado = true, DestinoEnmascarado = enmascarado };
    }
}
