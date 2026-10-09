using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Dominio.Entidades.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Auth;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _repositorio;
    private readonly IMapper _mapper;
    private readonly ILogger<MenuService> _logger;

    public MenuService(IMenuRepository repositorio, IMapper mapper, ILogger<MenuService> logger)
    {
        _repositorio = repositorio;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<MenuDto>> ObtenerTodosAsync(GenericPaginationQueryDto query, int? sistemaId = null)
    {
        try
        {
            var baseQuery = _repositorio.AsQueryable();

            if (sistemaId.HasValue)
            {
                baseQuery = baseQuery.Where(m => m.SistemaId == sistemaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var filterLower = query.Filter.ToLower();
                baseQuery = baseQuery.Where(m => m.Nombre!.ToLower().Contains(filterLower));
            }

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);

            var menus = await filteredQuery.ToListAsync();
            var dtos = _mapper.Map<List<MenuDto>>(menus);

            return new PagedResultDto<MenuDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener menús");
            throw;
        }
    }

    public async Task<List<MenuDto>> ObtenerActivosAsync(int? sistemaId = null)
    {
        try
        {
            var menus = await _repositorio.ObtenerActivosAsync(sistemaId);
            return _mapper.Map<List<MenuDto>>(menus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener menús activos");
            throw;
        }
    }

    public async Task<MenuDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var menu = await _repositorio.ObtenerPorIdAsync(id);
            return menu == null ? null : _mapper.Map<MenuDto>(menu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener menú {Id}", id);
            throw;
        }
    }

    public async Task<MenuDto> CrearAsync(CrearMenuDto dto)
    {
        try
        {
            var menu = _mapper.Map<Menu>(dto);
            menu.CreatedAt = DateTime.UtcNow;
            var resultado = await _repositorio.CrearAsync(menu);
            return _mapper.Map<MenuDto>(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear menú");
            throw;
        }
    }

    public async Task<MenuDto> ActualizarAsync(MenuDto dto)
    {
        try
        {
            var menu = await _repositorio.ObtenerPorIdAsync(dto.Id);
            if (menu == null)
                throw new Exception("Menú no encontrado");
            
            menu.Nombre = dto.Nombre;
            menu.Url = dto.Url;
            menu.Icono = dto.Icono;
            menu.Orden = dto.Orden ?? 0;
            menu.IsGroup = dto.IsGroup;
            menu.ParentId = dto.ParentId;
            menu.SistemaId = dto.SistemaId;
            
            var resultado = await _repositorio.ActualizarAsync(menu);
            return _mapper.Map<MenuDto>(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar menú {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            await _repositorio.EliminarAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar menú {Id}", id);
            throw;
        }
    }

    public async Task<List<MenuArbolDto>> ObtenerArbolAsync(int? sistemaId = null)
    {
        try
        {
            var menus = await _repositorio.ObtenerActivosAsync(sistemaId);
            return ConstruirArbol(menus, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener árbol de menús");
            throw;
        }
    }

    private static List<MenuArbolDto> ConstruirArbol(List<Menu> todosMenus, int? parentId)
    {
        return todosMenus
            .Where(m => m.ParentId == parentId)
            .OrderBy(m => m.Orden)
            .Select(m =>
            {
                var hijos = ConstruirArbol(todosMenus, m.Id);

                if (m.IsGroup)
                {
                    return new MenuArbolDto
                    {
                        MenuId = m.Id,
                        Nombre = m.Nombre,
                        Icono = m.Icono,
                        Orden = m.Orden,
                        IsGroup = true,
                        SistemaId = m.SistemaId,
                        SistemaNombre = m.Sistema?.Nombre,
                        SubMenus = hijos
                    };
                }

                return new MenuArbolDto
                {
                    MenuId = m.Id,
                    Nombre = m.Nombre,
                    Url = m.Url,
                    Icono = m.Icono,
                    Orden = m.Orden,
                    SistemaId = m.SistemaId,
                    SistemaNombre = m.Sistema?.Nombre,
                    Leer = true,
                    Crear = true,
                    Editar = true,
                    Eliminar = true
                };
            })
            .ToList();
    }
}