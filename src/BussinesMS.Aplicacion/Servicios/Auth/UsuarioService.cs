using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BussinesMS.Aplicacion.Servicios.Auth;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repositorio;
    private readonly IMenuRepository _menuRepositorio;
    private readonly IMapper _mapper;
    private readonly ILogger<UsuarioService> _logger;
    private readonly JwtHelper _jwtHelper;
    private readonly IRolRepository _rolRepositorio;
    private readonly ICurrentUserService _currentUser;
    private readonly ISistemaRepository _sistemaRepositorio;

    public UsuarioService(
        IUsuarioRepository repositorio,
        IMenuRepository menuRepositorio,
        IMapper mapper,
        ILogger<UsuarioService> logger,
        JwtHelper jwtHelper,
        IRolRepository rolRepositorio,
        ICurrentUserService currentUser,
        ISistemaRepository sistemaRepositorio)
    {
        _repositorio = repositorio;
        _menuRepositorio = menuRepositorio;
        _mapper = mapper;
        _logger = logger;
        _jwtHelper = jwtHelper;
        _rolRepositorio = rolRepositorio;
        _currentUser = currentUser;
        _sistemaRepositorio = sistemaRepositorio;
    }

    public async Task<PagedResultDto<UsuarioDto>> ObtenerTodosAsync(UsuarioFiltroDto query)
    {
        try
        {
            var baseQuery = _repositorio.AsQueryable();

            if (query.IsActive.HasValue)
                baseQuery = baseQuery.Where(u => u.IsActive == query.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var filterLower = query.Filter.ToLower();
                baseQuery = baseQuery.Where(u => u.Username!.ToLower().Contains(filterLower) ||
                                                 u.Nombre!.ToLower().Contains(filterLower));
            }

            // Acceso a un sistema: fila en UsuarioSistema, o sin filas y ese sistema como default
            if (query.SistemaId.HasValue)
            {
                var sistemaId = query.SistemaId.Value;
                baseQuery = baseQuery.Where(u => u.UsuarioSistemas.Any(us => us.SistemaId == sistemaId) ||
                                                 (!u.UsuarioSistemas.Any() && u.SistemaIdDefault == sistemaId));
            }

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);

            var usuarios = await filteredQuery.ToListAsync();
            var dtos = _mapper.Map<List<UsuarioDto>>(usuarios);

            var sistemasPorUsuario = await _repositorio.ObtenerSistemaIdsPorUsuariosAsync(usuarios.Select(u => u.Id).ToList());
            foreach (var dto in dtos)
                dto.SistemaIds = sistemasPorUsuario.TryGetValue(dto.Id, out var ids) ? ids : new List<int> { dto.SistemaIdDefault };

            return new PagedResultDto<UsuarioDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios");
            throw;
        }
    }

    public async Task<UsuarioConMenusDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var usuario = await _repositorio.ObtenerConRolAsync(id);
            if (usuario == null) return null;

            var sistemaIds = await ObtenerSistemaIdsUsuarioAsync(usuario);

            // Menús asignados de todos los sistemas del usuario, para que el editor no pierda los de otro sistema
            var menus = new List<MenuArbolDto>();
            foreach (var sistemaId in sistemaIds)
                menus.AddRange(await ConstruirArbolMenusAsync(usuario.Id, sistemaId));

            return new UsuarioConMenusDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Username = usuario.Username,
                SistemaIdDefault = usuario.SistemaIdDefault,
                SistemaIds = sistemaIds,
                RolId = usuario.RolId,
                RolNombre = usuario.Rol?.Nombre,
                IsActive = usuario.IsActive,
                CreatedAt = BoliviaTimeZone.ToLocal(usuario.CreatedAt),
                Menus = menus
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario {Id}", id);
            throw;
        }
    }

    public async Task<(UsuarioDto Entidad, bool FueReactivada)> CrearAsync(CrearUsuarioDto dto)
    {
        try
        {
            var duplicado = await _repositorio.ObtenerPorUsernameAsync(dto.Username);

            if (duplicado != null)
            {
                if (duplicado.IsActive)
                    throw new InvalidOperationException($"El usuario '{dto.Username}' ya existe.");

                var reactivado = await _repositorio.ReactivarAsync(duplicado.Id);
                _logger.LogInformation("Usuario reactivado: {Username}", reactivado.Username);
                return (_mapper.Map<UsuarioDto>(reactivado), true);
            }

            var sistemaIds = await ResolverSistemaIdsAsync(dto.SistemaIds, dto.SistemaIdDefault);

            var usuario = _mapper.Map<Usuario>(dto);
            usuario.PasswordHash = HashPassword(dto.Password);
            usuario.CreatedAt = DateTime.UtcNow;

            var resultado = await _repositorio.CrearAsync(usuario);
            await _repositorio.ReemplazarSistemasAsync(resultado.Id, sistemaIds);

            List<MenuPermisoSimpleDto> menusAsignar;

            if (dto.Menus != null && dto.Menus.Count > 0)
            {
                menusAsignar = dto.Menus;
            }
            else
            {
                var rol = await _repositorio.ObtenerConRolAsync(resultado.Id);
                if (rol?.Rol != null && !string.IsNullOrEmpty(rol.Rol.MenuIds))
                {
                    var menuIds = JsonSerializer.Deserialize<List<int>>(rol.Rol.MenuIds) ?? new List<int>();

                    // La plantilla del rol solo tiene menús del regular: se aplican solo los de los sistemas del usuario
                    var menuIdsDeSistemas = (await _menuRepositorio.ObtenerTodosAsync())
                        .Where(m => m.SistemaId.HasValue && sistemaIds.Contains(m.SistemaId.Value))
                        .Select(m => m.Id)
                        .ToHashSet();
                    menuIds = menuIds.Where(menuIdsDeSistemas.Contains).ToList();

                    menusAsignar = menuIds.Select(menuId => new MenuPermisoSimpleDto
                    {
                        MenuId = menuId,
                        Leer = true,
                        Crear = true,
                        Editar = true,
                        Eliminar = true
                    }).ToList();
                }
                else
                {
                    menusAsignar = new List<MenuPermisoSimpleDto>();
                }
            }

            if (menusAsignar.Count > 0)
            {
                var menusEntidad = menusAsignar.Select(m => new UsuarioMenu
                {
                    UsuarioId = resultado.Id,
                    MenuId = m.MenuId,
                    Leer = m.Leer,
                    Crear = m.Crear,
                    Editar = m.Editar,
                    Eliminar = m.Eliminar,
                    PermisosEspeciales = m.PermisosEspeciales
                }).ToList();

                await _repositorio.AgregarMenusAsync(resultado.Id, menusEntidad);
            }

            return (_mapper.Map<UsuarioDto>(resultado), false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear usuario");
            throw;
        }
    }

    public async Task<UsuarioDto> ActualizarAsync(int id, ActualizarUsuarioDto dto)
    {
        try
        {
            var usuario = await _repositorio.ObtenerPorIdAsync(id);
            if (usuario == null)
                throw new KeyNotFoundException("Usuario no encontrado");

            if (string.IsNullOrWhiteSpace(dto.Username))
                throw new ArgumentException("El username es obligatorio.");

            var duplicado = await _repositorio.ObtenerPorUsernameAsync(dto.Username);
            if (duplicado != null && duplicado.Id != id)
                throw new InvalidOperationException($"El username '{dto.Username}' ya está en uso por otro usuario.");

            var rol = await _rolRepositorio.ObtenerPorIdAsync(dto.RolId);
            if (rol == null || !rol.IsActive)
                throw new ArgumentException($"El rol {dto.RolId} no existe o está inactivo.");

            usuario.Nombre = dto.Nombre;
            usuario.Apellido = dto.Apellido;
            usuario.Email = dto.Email;
            usuario.SistemaIdDefault = dto.SistemaIdDefault;
            usuario.Username = dto.Username;
            usuario.RolId = dto.RolId;
            usuario.UpdatedAt = DateTime.UtcNow;

            // Si no vienen SistemaIds se conservan los actuales (más el default), para no quitar accesos
            // a un usuario editado desde un front que todavía no manda el campo.
            var sistemaIds = dto.SistemaIds is { Count: > 0 }
                ? await ResolverSistemaIdsAsync(dto.SistemaIds, dto.SistemaIdDefault)
                : await ResolverSistemaIdsAsync(await _repositorio.ObtenerSistemaIdsAsync(id), dto.SistemaIdDefault);

            var resultado = await _repositorio.ActualizarAsync(usuario);
            await _repositorio.ReemplazarSistemasAsync(id, sistemaIds);
            return _mapper.Map<UsuarioDto>(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario {Id}", id);
            throw;
        }
    }

    public async Task<List<MenuArbolDto>> ObtenerMenusAsync(int id)
    {
        try
        {
            var usuario = await _repositorio.ObtenerConRolAsync(id);
            if (usuario == null)
                return new List<MenuArbolDto>();

            return await ConstruirArbolMenusAsync(usuario.Id, usuario.SistemaIdDefault);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener menús del usuario {Id}", id);
            throw;
        }
    }

    public async Task<UsuarioDto> ActualizarMenusAsync(int id, List<MenuPermisoSimpleDto> menus)
    {
        try
        {
            var usuario = await _repositorio.ObtenerConRolAsync(id);
            if (usuario == null)
                throw new Exception("Usuario no encontrado");

            var sistemaIds = await ObtenerSistemaIdsUsuarioAsync(usuario);
            var menusDelSistema = (await _menuRepositorio.ObtenerActivosAsync())
                .Where(m => m.SistemaId.HasValue && sistemaIds.Contains(m.SistemaId.Value));
            var menuIdsDelSistema = menusDelSistema.Select(m => m.Id).ToHashSet();

            var menusValidos = menus.Where(m => menuIdsDelSistema.Contains(m.MenuId)).ToList();

            if (menusValidos.Count != menus.Count)
            {
                var menusInvalidos = menus.Where(m => !menuIdsDelSistema.Contains(m.MenuId)).Select(m => m.MenuId).ToList();
                throw new Exception($"Los siguientes menús no pertenecen a los sistemas del usuario ({string.Join(", ", sistemaIds)}): {string.Join(", ", menusInvalidos)}");
            }

            await _repositorio.EliminarMenusAsync(id);

            var usuarioMenus = menusValidos.Select(m => new UsuarioMenu
            {
                UsuarioId = id,
                MenuId = m.MenuId,
                Leer = m.Leer,
                Crear = m.Crear,
                Editar = m.Editar,
                Eliminar = m.Eliminar,
                PermisosEspeciales = m.PermisosEspeciales
            }).ToList();

            await _repositorio.AgregarMenusAsync(id, usuarioMenus);

            return _mapper.Map<UsuarioDto>(usuario);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar menús del usuario {Id}", id);
            throw;
        }
    }

    public async Task<LoginResponseDto?> ValidarLoginAsync(string username, string password, int? sistemaId = null)
    {
        try
        {
            var usuario = await _repositorio.ObtenerConRolAsyncPorUsername(username);
            if (usuario == null) return null;

            if (!VerificarPassword(password, usuario.PasswordHash))
                return null;

            if (!usuario.IsActive)
                throw new ExcepcionDominio("Usuario inactivo", 403, "USUARIO_INACTIVO");

            // Sin sistema elegido se usa el default, igual que antes
            var sistemaElegido = sistemaId ?? usuario.SistemaIdDefault;
            if (sistemaId.HasValue)
            {
                var sistemaIds = await _repositorio.ObtenerSistemaIdsAsync(usuario.Id);
                if (!sistemaIds.Contains(sistemaId.Value))
                    throw new ExcepcionDominio("No tiene acceso a este sistema", 403, "SISTEMA_SIN_ACCESO");
            }

            var sistema = await _sistemaRepositorio.ObtenerPorIdAsync(sistemaElegido);

            var token = _jwtHelper.GenerateToken(usuario.Id, usuario.Username, usuario.RolId, sistemaElegido);
            var menus = await ConstruirArbolMenusAsync(usuario.Id, sistemaElegido);

            return new LoginResponseDto
            {
                Usuario = _mapper.Map<UsuarioDto>(usuario),
                Token = token,
                RolNombre = usuario.Rol?.Nombre,
                SistemaId = sistemaElegido,
                SistemaNombre = sistema?.Nombre,
                Menus = menus
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar login para usuario {Username}", username);
            throw;
        }
    }

    public async Task CambiarPasswordAsync(int id, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña es obligatoria.");

            var usuario = await _repositorio.ObtenerPorIdAsync(id);
            if (usuario == null)
                throw new KeyNotFoundException("Usuario no encontrado");

            usuario.PasswordHash = HashPassword(password);
            usuario.UpdatedAt = DateTime.UtcNow;

            await _repositorio.ActualizarAsync(usuario);
            _logger.LogInformation("Contraseña reseteada para usuario {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar la contraseña del usuario {Id}", id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var usuario = await _repositorio.ObtenerPorIdAsync(id);
            if (usuario == null)
                throw new KeyNotFoundException("Usuario no encontrado");

            if (_currentUser.GetUsuarioId() == id)
                throw new InvalidOperationException("No puedes desactivar tu propio usuario.");

            if (!usuario.IsActive)
                throw new InvalidOperationException("El usuario ya está inactivo.");

            await _repositorio.EliminarAsync(id);
            _logger.LogInformation("Usuario desactivado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desactivar usuario {Id}", id);
            throw;
        }
    }

    public async Task ReactivarAsync(int id)
    {
        try
        {
            var usuario = await _repositorio.ObtenerPorIdAsync(id);
            if (usuario == null)
                throw new KeyNotFoundException("Usuario no encontrado");

            if (usuario.IsActive)
                throw new InvalidOperationException("El usuario ya está activo.");

            await _repositorio.ReactivarAsync(id);
            _logger.LogInformation("Usuario reactivado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reactivar usuario {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Sistemas a los que tiene acceso el usuario (UsuarioSistema). Si no tiene filas, cae al SistemaIdDefault.
    /// </summary>
    private async Task<List<int>> ObtenerSistemaIdsUsuarioAsync(Usuario usuario)
    {
        var sistemaIds = await _repositorio.ObtenerSistemaIdsAsync(usuario.Id);
        return sistemaIds.Count > 0 ? sistemaIds : new List<int> { usuario.SistemaIdDefault };
    }

    /// <summary>
    /// Normaliza la lista de sistemas: si no viene usa [SistemaIdDefault], siempre incluye el default
    /// y valida que todos existan y estén activos.
    /// </summary>
    private async Task<List<int>> ResolverSistemaIdsAsync(List<int>? sistemaIds, int sistemaIdDefault)
    {
        var resultado = sistemaIds is { Count: > 0 } ? sistemaIds.Distinct().ToList() : new List<int>();
        if (!resultado.Contains(sistemaIdDefault))
            resultado.Insert(0, sistemaIdDefault);

        foreach (var sistemaId in resultado)
        {
            var sistema = await _sistemaRepositorio.ObtenerPorIdAsync(sistemaId);
            if (sistema == null || !sistema.IsActive)
                throw new ArgumentException($"El sistema {sistemaId} no existe o está inactivo.");
        }

        return resultado.OrderBy(id => id).ToList();
    }

    private async Task<List<MenuArbolDto>> ConstruirArbolMenusAsync(int usuarioId, int sistemaId)
    {
        var usuarioMenus = await _repositorio.ObtenerMenusAsync(usuarioId);
        var todosMenus = await _menuRepositorio.ObtenerActivosAsync(sistemaId);

        var menusAsignados = usuarioMenus.ToDictionary(um => um.MenuId);
        var todosMenusDict = todosMenus.ToDictionary(m => m.Id);

        var resultado = new List<MenuArbolDto>();

        foreach (var menu in todosMenus.Where(m => m.ParentId == null).OrderBy(m => m.Orden))
        {
            var nodo = ConstruirNodo(menu, todosMenus, menusAsignados, todosMenusDict);
            if (nodo != null)
                resultado.Add(nodo);
        }

        return resultado;
    }

    private MenuArbolDto? ConstruirNodo(
        Menu menu,
        List<Menu> todosMenus,
        Dictionary<int, UsuarioMenu> menusAsignados,
        Dictionary<int, Menu> todosMenusDict)
    {
        var esAsignado = menusAsignados.ContainsKey(menu.Id);

        var hijos = todosMenus
            .Where(m => m.ParentId == menu.Id)
            .OrderBy(m => m.Orden)
            .Select(hijo => ConstruirNodo(hijo, todosMenus, menusAsignados, todosMenusDict))
            .Where(h => h != null)
            .ToList();

        if (!esAsignado && hijos.Count == 0)
            return null;

        var usuarioMenu = esAsignado ? menusAsignados[menu.Id] : null;

        if (menu.IsGroup)
        {
            return new MenuArbolDto
            {
                MenuId = menu.Id,
                Nombre = menu.Nombre,
                Icono = menu.Icono,
                Orden = menu.Orden,
                IsGroup = true,
                SistemaId = menu.SistemaId,
                SistemaNombre = menu.Sistema?.Nombre,
                SubMenus = hijos!
            };
        }

        var permisosEspeciales = usuarioMenu?.PermisosEspeciales != null
            ? JsonSerializer.Deserialize<string[]>(usuarioMenu.PermisosEspeciales) ?? null
            : null;

        return new MenuArbolDto
        {
            MenuId = menu.Id,
            Nombre = menu.Nombre,
            Url = menu.Url,
            Icono = menu.Icono,
            Orden = menu.Orden,
            SistemaId = menu.SistemaId,
            SistemaNombre = menu.Sistema?.Nombre,
            Leer = usuarioMenu?.Leer ?? false,
            Crear = usuarioMenu?.Crear ?? false,
            Editar = usuarioMenu?.Editar ?? false,
            Eliminar = usuarioMenu?.Eliminar ?? false,
            Permisos = permisosEspeciales
        };
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerificarPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
