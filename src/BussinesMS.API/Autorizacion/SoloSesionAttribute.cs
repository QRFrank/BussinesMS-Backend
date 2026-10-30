namespace BussinesMS.API.Autorizacion;

/// <summary>Permisos fase 2: alcanza con estar logueado (lookups, catálogos y endpoints que el service ya valida por rol).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SoloSesionAttribute : Attribute
{
}
