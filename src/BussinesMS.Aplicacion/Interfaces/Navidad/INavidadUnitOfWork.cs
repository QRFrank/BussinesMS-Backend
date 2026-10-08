namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface INavidadUnitOfWork
{
    Task BeginTransactionAsync();
    Task<int> SaveChangesAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
