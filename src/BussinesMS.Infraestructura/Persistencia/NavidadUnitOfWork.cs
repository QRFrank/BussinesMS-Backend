using BussinesMS.Aplicacion.Interfaces.Navidad;

namespace BussinesMS.Infraestructura.Persistencia;

public class NavidadUnitOfWork : INavidadUnitOfWork
{
    private readonly NavidadDbContext _context;
    private readonly IBandejaSenal _senal;
    private bool _avisosPendientes;

    public NavidadUnitOfWork(NavidadDbContext context, IBandejaSenal senal)
    {
        _context = context;
        _senal = senal;
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
        // Recién confirmada la transacción se despierta la bandeja (para que lea los avisos ya visibles)
        if (_avisosPendientes)
        {
            _avisosPendientes = false;
            _senal.Despertar();
        }
    }

    public async Task RollbackAsync()
    {
        _avisosPendientes = false;
        await _context.Database.RollbackTransactionAsync();
    }

    public void MarcarAvisosPendientes()
    {
        if (_context.Database.CurrentTransaction == null)
            _senal.Despertar();
        else
            _avisosPendientes = true;
    }
}
