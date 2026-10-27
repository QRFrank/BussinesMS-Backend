using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Operaciones en memoria sobre bloqueos ya cargados con tracking; el llamador guarda con INavidadUnitOfWork.SaveChangesAsync.
// "Eliminar" = borrado lógico (IsActive = false); bajar una parte reduce Unidades (la columna exige Unidades > 0).
internal static class ReservaBloqueoHelper
{
    internal static void Desactivar(ReservaBloqueo b, int usuarioId, DateTime ahora)
    {
        b.IsActive = false;
        b.DeletedAt = ahora;
        b.DeletedByUsuarioId = usuarioId;
        b.UpdatedAt = ahora;
        b.UpdatedByUsuarioId = usuarioId;
    }

    // Baja hasta `unidades` de los bloqueos dados, empezando por el más antiguo. Devuelve lo realmente liberado
    internal static int Liberar(IEnumerable<ReservaBloqueo> bloqueos, int unidades, int usuarioId, DateTime ahora)
    {
        var restante = unidades;
        foreach (var b in bloqueos.Where(x => x.IsActive).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
        {
            if (restante <= 0) break;
            var quitar = Math.Min(restante, b.Unidades);
            if (quitar == b.Unidades)
                Desactivar(b, usuarioId, ahora);
            else
            {
                b.Unidades -= quitar;
                b.UpdatedAt = ahora;
                b.UpdatedByUsuarioId = usuarioId;
            }
            restante -= quitar;
        }
        return unidades - restante;
    }
}
