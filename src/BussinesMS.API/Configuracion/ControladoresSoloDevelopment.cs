using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace BussinesMS.API.Configuracion;

/// <summary>
/// Fuera de Development quita estos controllers del MVC: sus rutas no existen y responden 404.
/// (MigracionesController tiene un DELETE LimpiarDatos que borra datos del sistema regular.)
/// </summary>
public class ControladoresSoloDevelopment : IApplicationFeatureProvider<ControllerFeature>
{
    private static readonly Type[] Tipos = [typeof(Controllers.MigracionesController)];

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var tipo in Tipos)
        {
            var info = feature.Controllers.FirstOrDefault(t => t.AsType() == tipo);
            if (info != null) feature.Controllers.Remove(info);
        }
    }
}
