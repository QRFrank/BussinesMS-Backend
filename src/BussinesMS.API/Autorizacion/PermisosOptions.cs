namespace BussinesMS.API.Autorizacion;

public enum ModoPermisos
{
    /// <summary>No rechaza nada: registra en PermisoAuditoria lo que habría sido un 403.</summary>
    Aviso = 0,
    /// <summary>Rechaza con 403 JSON (y también lo registra).</summary>
    Bloqueo = 1
}

/// <summary>Sección "Permisos" de appsettings. Por defecto Modo = Aviso.</summary>
public class PermisosOptions
{
    public const string Seccion = "Permisos";

    public ModoPermisos Modo { get; set; } = ModoPermisos.Aviso;
}
