using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Infraestructura.Seed;

public class AuthSeeder
{
    private readonly AuthDbContext _context;
    private readonly ILogger<AuthSeeder> _logger;

    private static readonly string SeedPath = Path.Combine(
        AppContext.BaseDirectory, "Seed");

    public AuthSeeder(AuthDbContext context, ILogger<AuthSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EjecutarAsync()
    {
        try
        {
            await SeedSistemasAsync();
            await SeedAlmacenesAsync();
            await SeedMenusAsync();
            await SeedRolesAsync();
            await SeedUsuariosAsync();
            await SeedAccesoNavidadAdminAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando AuthSeeder");
            throw;
        }
    }

    private async Task SeedSistemasAsync()
    {
        if (await _context.Sistemas.AnyAsync()) return;

        _context.Sistemas.AddRange(
            new Sistema { Nombre = "Regular", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Sistema { Nombre = "Navideño", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();
        _logger.LogInformation("Sistemas insertados");
    }

    private async Task SeedAlmacenesAsync()
    {
        if (await _context.Almacenes.AnyAsync()) return;

        _context.Almacenes.AddRange(
            new Almacen { Nombre = "Tienda Principal", Codigo = "TP001", EsTienda = true, Direccion = "Centro", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Almacen { Nombre = "Almacén 1", Codigo = "A1001", EsTienda = false, Direccion = "Zona Industrial", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Almacen { Nombre = "Almacén 2", Codigo = "A2001", EsTienda = false, Direccion = "Zona Sur", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();
        _logger.LogInformation("Almacenes insertados");
    }

    private async Task SeedMenusAsync()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(SeedPath, "menus.json"));
        var items = JsonSerializer.Deserialize<List<MenuSeedDto>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        // Idempotente: solo inserta los menús del json cuyo Id todavía no existe (ej. menús navideños nuevos)
        var idsExistentes = await _context.Menus.Select(m => m.Id).ToListAsync();
        items = items.Where(m => !idsExistentes.Contains(m.Id)).ToList();
        if (items.Count == 0) return;

        var sinPadre = items.Where(m => m.ParentId == null).ToList();
        var conPadre = items.Where(m => m.ParentId != null).ToList();

        foreach (var item in sinPadre)
            _context.Menus.Add(MapMenu(item));

        await _context.SaveChangesAsync();

        foreach (var item in conPadre)
            _context.Menus.Add(MapMenu(item));

        await _context.SaveChangesAsync();
        _logger.LogInformation("Menús insertados: {Count}", items.Count);
    }

    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(Path.Combine(SeedPath, "roles.json"));
        var items = JsonSerializer.Deserialize<List<RolSeedDto>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        foreach (var item in items)
        {
            _context.Roles.Add(new Rol
            {
                Nombre = item.Nombre,
                MenuIds = JsonSerializer.Serialize(item.MenuIds),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Roles insertados: {Count}", items.Count);
    }

    private async Task SeedUsuariosAsync()
    {
        if (await _context.Usuarios.AnyAsync()) return;

        var json = await File.ReadAllTextAsync(Path.Combine(SeedPath, "usuarios.json"));
        var items = JsonSerializer.Deserialize<List<UsuarioSeedDto>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var roles = await _context.Roles.ToListAsync();
        var menus = await _context.Menus.Where(m => !m.IsGroup && m.IsActive).ToListAsync();

        foreach (var item in items)
        {
            var rol = roles.FirstOrDefault(r => r.Nombre == item.RolNombre)
                ?? throw new Exception($"Rol '{item.RolNombre}' no encontrado en seed");

            var menuIds = string.IsNullOrEmpty(rol.MenuIds)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(rol.MenuIds)!;

            var usuario = new Usuario
            {
                Nombre = item.Nombre,
                Apellido = item.Apellido,
                Email = item.Email,
                Username = item.Username,
                PasswordHash = HashPassword(item.Password),
                SistemaIdDefault = item.SistemaIdDefault,
                RolId = rol.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var menusDicts = menus
                .Where(m => menuIds.Contains(m.Id))
                .Select(m => new UsuarioMenu
                {
                    UsuarioId = usuario.Id,
                    MenuId = m.Id,
                    Leer = true,
                    Crear = true,
                    Editar = true,
                    Eliminar = true
                });

            _context.UsuarioMenus.AddRange(menusDicts);
            _context.UsuarioSistemas.Add(new UsuarioSistema { UsuarioId = usuario.Id, SistemaId = usuario.SistemaIdDefault });
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Usuarios insertados: {Count}", items.Count);
    }

    /// <summary>
    /// Idempotente: el admin del seed tiene acceso al sistema Navideño (2) y a todos sus menús activos.
    /// </summary>
    private async Task SeedAccesoNavidadAdminAsync()
    {
        const int sistemaNavidadId = 2;
        const string adminUsername = "admin";

        var admin = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == adminUsername);
        if (admin == null) return;
        if (!await _context.Sistemas.AnyAsync(s => s.Id == sistemaNavidadId)) return;

        var sistemasAdmin = await _context.UsuarioSistemas
            .Where(us => us.UsuarioId == admin.Id)
            .Select(us => us.SistemaId)
            .ToListAsync();

        foreach (var sistemaId in new[] { admin.SistemaIdDefault, sistemaNavidadId }.Distinct())
        {
            if (!sistemasAdmin.Contains(sistemaId))
                _context.UsuarioSistemas.Add(new UsuarioSistema { UsuarioId = admin.Id, SistemaId = sistemaId });
        }

        var menusNavidad = await _context.Menus
            .Where(m => m.SistemaId == sistemaNavidadId && m.IsActive && !m.IsGroup)
            .Select(m => m.Id)
            .ToListAsync();
        var menusAdmin = await _context.UsuarioMenus
            .Where(um => um.UsuarioId == admin.Id)
            .Select(um => um.MenuId)
            .ToListAsync();

        foreach (var menuId in menusNavidad.Where(id => !menusAdmin.Contains(id)))
        {
            _context.UsuarioMenus.Add(new UsuarioMenu
            {
                UsuarioId = admin.Id,
                MenuId = menuId,
                Leer = true,
                Crear = true,
                Editar = true,
                Eliminar = true
            });
        }

        await _context.SaveChangesAsync();
    }

    private static Menu MapMenu(MenuSeedDto dto) => new()
    {
        Id = dto.Id,
        Nombre = dto.Nombre,
        Url = dto.Url,
        Icono = dto.Icono,
        Orden = dto.Orden,
        IsGroup = dto.IsGroup,
        ParentId = dto.ParentId,
        SistemaId = dto.SistemaId,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private record MenuSeedDto(int Id, string Nombre, string? Url, string? Icono,
        int Orden, bool IsGroup, int? ParentId, int? SistemaId);

    private record RolSeedDto(string Nombre, List<int> MenuIds);

    private record UsuarioSeedDto(string Nombre, string Apellido, string? Email,
        string Username, string Password, int SistemaIdDefault, string RolNombre);
}
