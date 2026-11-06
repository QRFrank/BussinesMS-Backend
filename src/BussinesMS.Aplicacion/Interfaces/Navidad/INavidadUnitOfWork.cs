namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface INavidadUnitOfWork
{
    Task BeginTransactionAsync();
    Task<int> SaveChangesAsync();
    Task CommitAsync();
    Task RollbackAsync();

    /// <summary>
    /// Avisa que se encoló una notificación de WhatsApp. Con transacción abierta, despierta la bandeja recién después del Commit;
    /// sin transacción, de inmediato (ya está guardada).
    /// </summary>
    void MarcarAvisosPendientes();
}
