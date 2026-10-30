using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using BussinesMS.API.Autorizacion;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BussinesMS.API.Filtros;

// Permisos fase 2: en las acciones marcadas con [OcultarCamposSoloAdmin], si el usuario no es Admin,
// deja en null las propiedades de la respuesta marcadas con [OcultoSinAdmin] (ej. SueldoMensual).
// Mismo recorrido que OcultarPreciosCompraFilter.
public class OcultarCamposSoloAdminFilter : IAsyncResultFilter
{
    private const int ProfundidadMaxima = 8;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _ocultables = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _recorribles = new();

    private readonly IPermisoUsuarioService _permisos;

    public OcultarCamposSoloAdminFilter(IPermisoUsuarioService permisos)
    {
        _permisos = permisos;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor accion
            && (accion.MethodInfo.GetCustomAttribute<OcultarCamposSoloAdminAttribute>(true) != null
                || accion.ControllerTypeInfo.GetCustomAttribute<OcultarCamposSoloAdminAttribute>(true) != null)
            && context.Result is ObjectResult { Value: not null } resultado
            && !await _permisos.EsAdminAsync())
        {
            Ocultar(resultado.Value, 0);
        }

        await next();
    }

    private static void Ocultar(object? valor, int profundidad)
    {
        if (valor == null || profundidad > ProfundidadMaxima) return;

        var tipo = valor.GetType();
        if (tipo.IsPrimitive || tipo.IsEnum || valor is string || tipo.IsValueType) return;

        if (valor is IDictionary diccionario)
        {
            foreach (var item in diccionario.Values) Ocultar(item, profundidad + 1);
            return;
        }

        if (valor is IEnumerable lista)
        {
            foreach (var item in lista) Ocultar(item, profundidad + 1);
            return;
        }

        var nombreAssembly = tipo.Assembly.GetName().Name ?? string.Empty;
        if (!nombreAssembly.StartsWith("BussinesMS", StringComparison.Ordinal)) return;

        foreach (var p in _ocultables.GetOrAdd(tipo, t => t.GetProperties()
                     .Where(x => x.GetCustomAttribute<OcultoSinAdminAttribute>() != null && x.CanWrite)
                     .ToArray()))
        {
            p.SetValue(valor, null);
        }

        foreach (var p in _recorribles.GetOrAdd(tipo, t => t.GetProperties()
                     .Where(x => x.CanRead && x.GetIndexParameters().Length == 0
                                 && !x.PropertyType.IsPrimitive && !x.PropertyType.IsEnum
                                 && x.PropertyType != typeof(string) && !x.PropertyType.IsValueType
                                 && Nullable.GetUnderlyingType(x.PropertyType) == null)
                     .ToArray()))
        {
            Ocultar(p.GetValue(valor), profundidad + 1);
        }
    }
}
