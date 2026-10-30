namespace BussinesMS.API.Autorizacion;

/// <summary>
/// Permisos fase 2: el usuario necesita el permiso "Puede hacer recepciones" del sistema Navideño
/// (UsuarioSistema.PuedeHacerRecepciones) o ser Admin. Solo, aplica esa regla; junto a [Menu] en el
/// mismo miembro, es una alternativa: pasa con el flag del menú O con el permiso.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RequierePuedeHacerRecepcionesAttribute : Attribute
{
}
