using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BussinesMS.Infraestructura.Persistencia;

public class NavidadDbContextFactory : IDesignTimeDbContextFactory<NavidadDbContext>
{
    public NavidadDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NavidadDbContext>();
        optionsBuilder.UseSqlServer("Server=LAPTOP-NNA8N5KQ\\SQLEXPRESS;Database=BussinesMS_Navidad;User Id=sa;Password=12345678;TrustServerCertificate=True;");
        return new NavidadDbContext(optionsBuilder.Options);
    }
}
