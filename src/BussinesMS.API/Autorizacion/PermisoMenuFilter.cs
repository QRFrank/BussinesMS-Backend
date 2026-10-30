using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Dominio.Entidades.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace BussinesMS.API.Autorizacion;

/// <summary>
/// Permisos fase 2 (filtro global, mismo patrón que OcultarPreciosCompraFilter).
/// Regla por endpoint: [AllowAnonymous], [SoloSesion], [SoloAdmin] o [Menu(ids...)] (flag por verbo o explícito).
/// El Admin pasa siempre. Sin atributo = denegado (falla cerrada).
/// Modo Aviso (default): no rechaza, registra en PermisoAuditoria lo que habría sido 403. Modo Bloqueo: 403 JSON.
/// </summary>
public class PermisoMenuFilter : IAsyncAuthorizationFilter
{
    private readonly IPermisoUsuarioService _permisos;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PermisosOptions _opciones;
    private readonly ILogger<PermisoMenuFilter> _logger;

    public PermisoMenuFilter(
        IPermisoUsuarioService permisos,
        IServiceScopeFactory scopeFactory,
        IOptions<PermisosOptions> opciones,
        ILogger<PermisoMenuFilter> logger)
    {
        _permisos = permisos;
        _scopeFactory = scopeFactory;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor accion) return;

        var metodo = context.HttpContext.Request.Method;
        var regla = ReglaPermisoResolver.Resolver(accion, metodo);

        if (regla.Tipo == TipoReglaPermiso.Anonimo) return;
        // Sin sesión ya responde 401 la FallbackPolicy (fase 1); acá no se decide nada
        if (context.HttpContext.User.Identity?.IsAuthenticated != true) return;
        if (regla.Tipo == TipoReglaPermiso.SoloSesion) return;
        if (await _permisos.EsAdminAsync()) return;

        string mensaje;
        string motivo;
        string flagRequerido;

        switch (regla.Tipo)
        {
            case TipoReglaPermiso.Menu:
            {
                var flags = await _permisos.ObtenerFlagsAsync();
                var puedeRecepciones = regla.OPuedeHacerRecepciones && await _permisos.PuedeHacerRecepcionesAsync();
                if (EvaluadorPermiso.Permitido(regla, true, false, flags, puedeRecepciones)) return;

                var nombres = await _permisos.ObtenerNombresMenusAsync(regla.Menus);
                var textoMenus = string.Join(" o ", regla.Menus.Select(id => nombres.TryGetValue(id, out var n) ? n : $"menú {id}"));
                flagRequerido = regla.Flag.ToString();
                mensaje = $"No tenés permiso para {Accion(regla.Flag)} en {textoMenus}.";
                motivo = $"Sin {flagRequerido} en ningún menú de [{string.Join(",", regla.Menus)}]";
                if (regla.OPuedeHacerRecepciones) motivo += " ni el permiso Puede hacer recepciones";
                break;
            }
            case TipoReglaPermiso.PuedeHacerRecepciones:
                if (await _permisos.PuedeHacerRecepcionesAsync()) return;
                flagRequerido = "PuedeHacerRecepciones";
                mensaje = "No tenés permiso para hacer recepciones.";
                motivo = "Sin el permiso Puede hacer recepciones (sistema Navideño)";
                break;
            case TipoReglaPermiso.SoloAdmin:
                flagRequerido = "Admin";
                mensaje = "No tenés permiso para realizar esta acción: es solo para el administrador.";
                motivo = "Endpoint solo admin";
                break;
            default:
                flagRequerido = "SinMapear";
                mensaje = "No tenés permiso para realizar esta acción (el endpoint no tiene permisos configurados).";
                motivo = "Endpoint sin regla de permisos (falla cerrada)";
                break;
        }

        await RegistrarAsync(context, accion, regla, flagRequerido, motivo);

        if (_opciones.Modo == ModoPermisos.Bloqueo)
        {
            context.Result = new ObjectResult(new { success = false, message = mensaje, errorCode = "SIN_PERMISO" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    private static string Accion(FlagMenu flag) => flag switch
    {
        FlagMenu.Leer => "ver",
        FlagMenu.Crear => "crear",
        FlagMenu.Editar => "editar",
        FlagMenu.Eliminar => "eliminar o anular",
        _ => "realizar esta acción"
    };

    // En otro scope: el registro no debe mezclarse con los cambios del request ni romperlo si falla
    private async Task RegistrarAsync(AuthorizationFilterContext context, ControllerActionDescriptor accion,
        ReglaPermiso regla, string flagRequerido, string motivo)
    {
        var http = context.HttpContext;
        var user = http.User;
        int.TryParse(user.FindFirst("sub")?.Value, out var usuarioId);
        int? sistemaId = int.TryParse(user.FindFirst("sistemaId")?.Value, out var s) ? s : null;
        var endpoint = accion.AttributeRouteInfo?.Template ?? $"{accion.ControllerName}/{accion.ActionName}";

        _logger.LogWarning("PERMISOS [{Modo}] {Usuario} ({UsuarioId}) {Metodo} {Endpoint}: {Motivo}",
            _opciones.Modo, user.FindFirst("unique_name")?.Value, usuarioId, http.Request.Method, endpoint, motivo);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPermisoAuditoriaRepository>();
            await repo.RegistrarAsync(new PermisoAuditoria
            {
                Fecha = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Username = Recortar(user.FindFirst("unique_name")?.Value, 50),
                SistemaId = sistemaId,
                Metodo = Recortar(http.Request.Method, 10)!,
                Ruta = Recortar(http.Request.Path.Value ?? string.Empty, 400)!,
                Endpoint = Recortar(endpoint, 300)!,
                Menus = regla.Menus.Length > 0 ? Recortar(string.Join(",", regla.Menus), 100) : null,
                FlagRequerido = flagRequerido,
                Motivo = Recortar(motivo, 300)!,
                Modo = _opciones.Modo.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar la auditoría de permisos de {Endpoint}", endpoint);
        }
    }

    private static string? Recortar(string? texto, int max)
        => texto == null || texto.Length <= max ? texto : texto[..max];
}
