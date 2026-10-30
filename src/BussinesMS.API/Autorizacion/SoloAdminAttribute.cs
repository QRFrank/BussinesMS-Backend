namespace BussinesMS.API.Autorizacion;

/// <summary>Permisos fase 2: solo el rol Admin, sin importar los flags de UsuarioMenus.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SoloAdminAttribute : Attribute
{
}
