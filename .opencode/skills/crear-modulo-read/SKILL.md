---
name: crear-modulo-read
description: Crea un módulo de solo lectura (consultas) usando el patrón de SistemaVentas.
---

# Skill: crear-modulo-read

## Propósito
Crea un módulo de solo lectura (query operations) - sin escritura.

## Cuándo Usar
- "solo lectura", "solo consultas", "solo GET"
- Módulos de reportes, búsquedas, listados
- Visualización sin modificación

## Ubicación según el Sistema

El proyecto está organizado por sistemas. Debes colocar los archivos en la carpeta correcta según el sistema al que pertenece el módulo:

### Si el módulo es del sistema "Auth" (autenticación):
- Entidades → `src/BussinesMS.Dominio/Entidades/Auth/`
- DTOs → `src/BussinesMS.Aplicacion/DTOs/Auth/`
- Interfaces → `src/BussinesMS.Aplicacion/Interfaces/Auth/`
- Servicios → `src/BussinesMS.Aplicacion/Servicios/Auth/`
- Repositorios → `src/BussinesMS.Infraestructura/Repositorios/Auth/`
- Controller → `src/BussinesMS.API/Controllers/`

### Si el módulo es del sistema "Sistema" (negocio principal):
- Entidades → `src/BussinesMS.Dominio/Entidades/Sistema/`
- DTOs → `src/BussinesMS.Aplicacion/DTOs/Sistema/`
- Interfaces → `src/BussinesMS.Aplicacion/Interfaces/Sistema/`
- Servicios → `src/BussinesMS.Aplicacion/Servicios/Sistema/`
- Repositorios → `src/BussinesMS.Infraestructura/Repositorios/Sistema/`
- Controller → `src/BussinesMS.API/Controllers/`

### Si el módulo es del sistema "Navidad" (temporada navideña):
- Entidades → `src/BussinesMS.Dominio/Entidades/Navidad/`
- DTOs → `src/BussinesMS.Aplicacion/DTOs/Navidad/`
- Interfaces → `src/BussinesMS.Aplicacion/Interfaces/Navidad/`
- Servicios → `src/BussinesMS.Aplicacion/Servicios/Navidad/`
- Repositorios → `src/BussinesMS.Infraestructura/Repositorios/Navidad/`
- Controller → `src/BussinesMS.API/Controllers/`

**Importante**: Los archivos Compartidos (EntidadBase, IRepositorio<T>, PagedResultDto, etc.) permanecen en su ubicación actual.

## Estructura a Créar

### 1. Dominio - Entidad
**Ubicación**: `src/BussinesMS.Dominio/Entidades/`

### 2. Aplicacion - DTOs
**Ubicación**: `src/BussinesMS.Aplicacion/DTOs/`

**IMPORTANTE - Separación de DTOs**:

Para cada entidad, crear:
- `[NombreEntidad]Dto` - Para lectura (GET) - incluye Id
- `Crear[NombreEntidad]Dto` - Para creación (POST) - sin Id (si aplica)

**Ejemplo**:
```csharp
// ProductoDto.cs
public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}

public class CrearProductoDto
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}
```

- `[NombreEntidad]ConsultaDto.cs` (filtros)

### 3. Aplicacion - Interfaz Repository
**Ubicación**: `src/BussinesMS.Aplicacion/Interfaces/`
**Solo métodos de lectura**:
- `ObtenerPorIdAsync(int id)`
- `ObtenerTodosAsync()`

### 4. Aplicacion - Interfaz Service
**Ubicación**: `src/BussinesMS.Aplicacion/Interfaces/`

### 5. Aplicacion - Servicio
**Inyecta**: Solo repository, NO DbContext

### 6. Infraestructura - Repository
**Inyecta**: DbContext

### 6. Controller
**Ruta SEGÚN el sistema**:
- Sistema: `[Route("api/Sistema/[controller]")]`
- Navidad: `[Route("api/Navidad/[controller]")]`

**Solo GETs** con paginación:
- `GET /api/Sistema/[nombreentidad]?Page=1&PageSize=10&Filter=...`
- `GET /api/Sistema/[nombreentidad]/{id}`

**Parámetros de paginación** (GenericPaginationQueryDto):
- `Page` (default: 1)
- `PageSize` (default: 10, max: 100)
- `Filter` - texto libre para búsqueda
- `SortBy` - campo a ordenar
- `SortDirection` - "asc" o "desc"

**Respuesta**: `PagedResultDto<[NombreEntidad]Dto>`

## Ejemplo

```
Usuario: "Crear módulo Categorías solo lectura"
→ Skill: crear-modulo-read
→ Crear:
  - Categoria.cs
  - ICategoriaRepository (solo lectura)
  - CategoriaService
  - CategoriasController (solo GET)
```

## Reglas

1. **NO crear métodos write**: Sin Crear/Actualizar/Eliminar
2. **Service NO conoce DbContext**
3. **Repository SÍ conoce DbContext**

## 7. Mapeos AutoMapper (OBLIGATORIO)
Agregar en MappingProfile.cs:
```csharp
CreateMap<[Entidad], [Entidad]Dto>();
CreateMap<[Entidad]ConsultaDto, [Entidad]>();
```

## 8. Manejo de Errores (OBLIGATORIO)
Agregar try-catch con logging en el servicio.

## 9. Implementación de Paginación (OBLIGATORIO)

El servicio debe usar `GenericPaginationQueryDto` y retornar `PagedResultDto<T>`:

```csharp
// Interfaz
Task<PagedResultDto<[NombreEntidad]Dto>> ObtenerTodosAsync(GenericPaginationQueryDto query);

// Implementación
public async Task<PagedResultDto<[NombreEntidad]Dto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
{
    try
    {
        var baseQuery = _repo.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(query.Filter))
        {
            var filterLower = query.Filter.ToLower();
            baseQuery = baseQuery.Where(x => x.Nombre.ToLower().Contains(filterLower));
        }
        
        (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);
        var entidades = await filteredQuery.ToListAsync();
        var dtos = _mapper.Map<List<[NombreEntidad]Dto>>(entidades);

        // ⚠️ OBLIGATORIO si el DTO expone CreatedAt/fechas: AutoMapper copia el valor
        // crudo en UTC (así se guarda en la base). Convertir siempre a hora de Bolivia:
        // using BussinesMS.Aplicacion.Common; (BoliviaTimeZone)
        for (int i = 0; i < dtos.Count; i++)
            dtos[i].CreatedAt = BoliviaTimeZone.ToLocal(entidades[i].CreatedAt);

        return new PagedResultDto<[NombreEntidad]Dto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.GetPageValue(),
            PageSize = query.GetPageSizeValue()
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al obtener [entidades]");
        throw;
    }
}
```