namespace BussinesMS.Aplicacion.Seguridad;

// Marca un campo que solo ve el Admin (permisos fase 2, ej. SueldoMensual). Solo se oculta en las acciones
// marcadas con [OcultarCamposSoloAdmin] en la API. La propiedad tiene que ser nullable.
[AttributeUsage(AttributeTargets.Property)]
public class OcultoSinAdminAttribute : Attribute
{
}
