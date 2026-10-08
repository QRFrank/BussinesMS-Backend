using BussinesMS.Aplicacion.Interfaces.Navidad;

namespace BussinesMS.Infraestructura.Persistencia;

public class NavidadUnitOfWork : INavidadUnitOfWork
{
    private readonly NavidadDbContext _context;

    public NavidadUnitOfWork(NavidadDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task CommitAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    public async Task RollbackAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }
}
