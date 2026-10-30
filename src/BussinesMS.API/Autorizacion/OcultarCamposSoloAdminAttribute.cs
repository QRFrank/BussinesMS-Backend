namespace BussinesMS.API.Autorizacion;

/// <summary>
/// En las respuestas de esta acción (o controller), si el usuario no es Admin, quedan en null las propiedades
/// marcadas con [OcultoSinAdmin] (ej. SueldoMensual en GET /Navidad/Vendedores). Ver OcultarCamposSoloAdminFilter.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class OcultarCamposSoloAdminAttribute : Attribute
{
}
