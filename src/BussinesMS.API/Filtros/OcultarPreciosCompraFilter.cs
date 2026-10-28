using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BussinesMS.API.Filtros;

// Permiso "Ver precios de compra" (Navideño): si el usuario no lo tiene, deja en null los campos de las
// respuestas marcados con [OcultoSinPrecioCompra]. Solo actúa en los controllers de Controllers/Navidad.
public class OcultarPreciosCompraFilter : IAsyncResultFilter
{
    private const string NamespaceNavidad = "BussinesMS.API.Controllers.Navidad";
    private const int ProfundidadMaxima = 8;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _ocultables = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _recorribles = new();

    private readonly IPrecioCompraAccesoService _acceso;

    public OcultarPreciosCompraFilter(IPrecioCompraAccesoService acceso)
    {
        _acceso = acceso;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var tipoController = context.Controller.GetType();
        if (tipoController.Namespace == NamespaceNavidad
            && context.Result is ObjectResult { Value: not null } resultado
            && !await _acceso.PuedeVerPreciosCompraAsync())
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
                     .Where(x => x.GetCustomAttribute<OcultoSinPrecioCompraAttribute>() != null && x.CanWrite)
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
