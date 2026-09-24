---
name: crear-modulo-crud
description: Crea un módulo completo CRUD siguiendo clean architecture. Repositorios retornan entidades, el Service mapea a DTOs. Usa excepciones de dominio tipadas. IEntidadActivable para validaciones type-safe.
---

# Skill: crear-modulo-crud

## ⚠️ REGLAS CRÍTICAS — LEER ANTES DE ESCRIBIR CUALQUIER ARCHIVO

Estas reglas corrigen errores detectados en módulos anteriores. Son obligatorias.

### REGLA 1 — Repositorio NUNCA retorna DTOs
Los métodos del repositorio retornan SIEMPRE entidades de dominio (`Categoria`, `Fabricante`, etc.).
El mapeo a DTO es responsabilidad EXCLUSIVA del Service.

```csharp
// ❌ MAL — repositorio retorna DTO
Task<List<CategoriaDto>> ObtenerRaicesAsync();

// ✅ BIEN — repositorio retorna entidad
Task<List<Categoria>> ObtenerRaicesAsync();
```

### REGLA 2 — Validaciones type-safe, sin reflexión
`ValidacionEntidad.VerificarActivo` NO debe usar `GetProperty("IsActive")`.
Todas las entidades implementan `IEntidadActivable`. El constraint genérico reemplaza la reflexión.

```csharp
// ❌ MAL — usa reflexión, frágil
var propiedad = entidad.GetType().GetProperty("IsActive");

// ✅ BIEN — constraint tipado
public static void VerificarActivo<T>(T? entidad, string nombre)
    where T : class, IEntidadActivable { ... }
```

### REGLA 3 — Mapeo consistente en el Service, y SIEMPRE convertir fechas a hora de Bolivia
En el Service, usar AutoMapper para TODOS los retornos cuando el DTO es un mapeo 1:1 de campos (sin
joins ni fechas). No mezclar mapeo manual con `_mapper.Map<>` sin motivo.

```csharp
// ❌ MAL — mapeo manual mezclado con mapper en entrada
var entidad = _mapper.Map<Categoria>(dto);  // usa mapper aquí
return new CategoriaDto { Id = creada.Id, Nombre = creada.Nombre }; // manual aquí

// ✅ BIEN — mapper en ambos sentidos
var entidad = _mapper.Map<Categoria>(dto);
var creada = await _repo.CrearAsync(entidad);
return _mapper.Map<CategoriaDto>(creada);
```

**Excepción obligatoria — campos `DateTime` en DTOs de lectura (`CreatedAt`, `FechaX`, etc.)**:
`AutoMapper` copia el valor crudo en UTC (así se guarda en la base, ver `EntidadBase.CreatedAt = DateTime.UtcNow`).
Todo DTO de lectura que exponga un campo de fecha DEBE convertirlo a hora de Bolivia con
`BussinesMS.Aplicacion.Common.BoliviaTimeZone.ToLocal(...)` antes de devolverlo — si no, la fecha
queda en UTC y es inconsistente con el resto de módulos que sí la convierten (bug confirmado en
`ProveedorService`, `SesionCajaService`, `MovimientoInventarioService`, `DevolucionClienteService`
y `TrasladoService`, que no convierten y muestran UTC crudo mientras el resto del sistema —
`CategoriaService`, `FabricanteService`, `DescripcionSaborService`, `DescripcionTamanioService`,
`CompraService`, `VentaService`, `ProductoVarianteService` — sí lo hace).

Dos formas válidas, elegir la que ensucie menos el código:

```csharp
// Opción A — sobreescribir el campo después de mapear (DTO simple, un solo registro)
var dto = _mapper.Map<CategoriaDto>(creada);
dto.CreatedAt = BoliviaTimeZone.ToLocal(creada.CreatedAt);
return dto;

// Opción B — construir el DTO a mano cuando ya hay varios campos de fecha o joins
// (ej. listados con Select(x => new XDto { ... })). En ese caso NO es una violación
// de esta regla, es el patrón esperado — no forzar _mapper.Map<> ahí.
var dtos = entidades.Select(c => new CategoriaDto
{
    Id = c.Id,
    Nombre = c.Nombre,
    CreatedAt = BoliviaTimeZone.ToLocal(c.CreatedAt),
    IsActive = c.IsActive
}).ToList();
```

### REGLA 4 — Excepciones de dominio tipadas (nunca `throw new Exception`)
Siempre usar las excepciones de `BussinesMS.Dominio.Excepciones`. El middleware las convierte al código HTTP correcto.

```csharp
// ❌ MAL
throw new Exception("Categoría padre no existe");

// ✅ BIEN
throw new EntidadNoEncontradaException("Categoría", id);
throw new EntidadDuplicadaException("categoría", dto.Nombre);
throw new ValidacionException("El campo X es inválido");
```

### REGLA 5 — Enum de negocio en Dominio, no en DTOs
Los enums de dominio van en `BussinesMS.Dominio.Enums`, no en `DTOs/Plantillas`.

```
// ❌ MAL
BussinesMS.Aplicacion/DTOs/Plantillas/GenericPaginationQueryDto.cs → enum TipoCategoriaFiltro

// ✅ BIEN
BussinesMS.Dominio/Enums/TipoCategoriaFiltro.cs
```

### REGLA 6 — EliminarAsync siempre soft-delete con usuario sistema
Si no hay usuario autenticado, usar usuario sistema (id=1). Nunca hacer delete físico en soft-delete.

```csharp
// ❌ MAL
if (usuarioId.HasValue) { softDelete } else { _dbSet.Remove(entidad); }

// ✅ BIEN
var usuarioId = _currentUser.GetUsuarioId() ?? 1;
entidad.DeletedByUsuarioId = usuarioId;
entidad.DeletedAt = DateTime.UtcNow;
entidad.IsActive = false;
_dbSet.Update(entidad);
```

### REGLA 7 — Controller: método Crear retorna 201 via BaseController
No usar `StatusCode(201, ...)` inline. Usar `RespuestaCreado<T>` de BaseController.

```csharp
// ❌ MAL
return StatusCode(201, new { Success = true, Data = resultado });

// ✅ BIEN
return RespuestaCreado(resultado, "[Entidad] creada");
```

### REGLA 8 — Enums de dominio van en Dominio
Si el módulo necesita un enum de filtro o estado, crearlo en `BussinesMS.Dominio/Enums/`.

### REGLA 9 — Transacciones con IUnitOfWork (CRITICAL)
Cuando el servicio tiene MÚLTIPLES OPERACIONES (subprocesos), DEBE usar IUnitOfWork.

**Cuándo Usar Transactions:**
- Crear + Actualizar (ej: crear producto, luego generar código interno)
- Crear + Crear (ej: venta + detalles de venta)
- Actualizar + Actualizar (ej: múltiples actualizaciones)
- Crear + Eliminar

**Patrón de Implementación:**
```csharp
public class [NombreEntidad]Service : I[NombreEntidad]Service
{
    private readonly I[NombreEntidad]Repository _repo;
    private readonly IUnitOfWork _uow;

    public [NombreEntidad]Service(
        I[NombreEntidad]Repository repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<[NombreEntidad]Dto> CrearAsync(Crear[NombreEntidad]Dto dto)
    {
        await _uow.BeginTransactionAsync();
        try
        {
            // Operación 1: Crear
            var entidad = new [NombreEntidad] { ... };
            var creada = await _repo.CrearAsync(entidad);

            // Operación 2: Actualizar (subproceso)
            creada.CodigoGenerado = $"COD-{creada.Id}";
            await _repo.ActualizarAsync(creada);

            await _uow.CommitAsync();
            return _mapper.Map<[NombreEntidad]Dto>(creada);
        }
        catch
        {
            await _uow.RollbackAsync();
            throw;
        }
    }
}
```

**Registro en Program.cs:**
```csharp
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## Ubicación según el Sistema

### Sistema "Auth":
- Entidades → `src/BussinesMS.Dominio/Entidades/Auth/`
- DTOs → `src/BussinesMS.Aplicacion/DTOs/Auth/`
- Interfaces → `src/BussinesMS.Aplicacion/Interfaces/Auth/`
- Servicios → `src/BussinesMS.Aplicacion/Servicios/Auth/`
- Repositorios → `src/BussinesMS.Infraestructura/Repositorios/Auth/`
- DbContext → `AuthDbContext`

### Sistema "Sistema" (negocio principal):
- Entidades → `src/BussinesMS.Dominio/Entidades/Sistema/`
- DTOs → `src/BussinesMS.Aplicacion/DTOs/Sistema/`
- Interfaces → `src/BussinesMS.Aplicacion/Interfaces/Sistema/`
- Servicios → `src/BussinesMS.Aplicacion/Servicios/Sistema/`
- Repositorios → `src/BussinesMS.Infraestructura/Repositorios/Sistema/`
- DbContext → `SistemaDbContext`

### Sistema "Navidad":
- Entidades → `src/BussinesMS.Dominio/Entidades/Navidad/`
- DTOs → `src/BussinesMS.Aplicacion/DTOs/Navidad/`
- Interfaces → `src/BussinesMS.Aplicacion/Interfaces/Navidad/`
- Servicios → `src/BussinesMS.Aplicacion/Servicios/Navidad/`
- Repositorios → `src/BussinesMS.Infraestructura/Repositorios/Navidad/`
- DbContext → `NavidadDbContext`

---

## Archivos Compartidos — VERIFICAR ANTES DE CREAR

Antes de crear cualquier archivo, verificar si estos ya existen. Si existen, NO recrearlos, solo referenciarlos:

- `src/BussinesMS.Dominio/Entidades/Compartido/EntidadBase.cs`
- `src/BussinesMS.Dominio/Interfaces/IEntidadActivable.cs` ← CREAR si no existe
- `src/BussinesMS.Dominio/Excepciones/ExcepcionesDominio.cs`
- `src/BussinesMS.Aplicacion/Interfaces/Compartido/IRepositorio.cs`
- `src/BussinesMS.Aplicacion/DTOs/Plantillas/PagedResultDto.cs`
- `src/BussinesMS.Aplicacion/DTOs/Plantillas/GenericPaginationQueryDto.cs`
- `src/BussinesMS.Aplicacion/Comun/QueryableExtensions.cs`
- `src/BussinesMS.Aplicacion/Helpers/ValidacionEntidad.cs`
- `src/BussinesMS.Aplicacion/Mapeos/MappingProfile.cs`
- `src/BussinesMS.Infraestructura/Repositorios/Compartido/RepositorioBase.cs`
- `src/BussinesMS.API/Controllers/BaseController.cs`
- `src/BussinesMS.API/Middlewares/ErrorHandlingMiddleware.cs`

---

## Paso 0 — Archivos compartidos a crear/corregir (solo si no existen o están incorrectos)

### 0.1 IEntidadActivable (CREAR si no existe)
**Ubicación**: `src/BussinesMS.Dominio/Interfaces/IEntidadActivable.cs`
```csharp
namespace BussinesMS.Dominio.Interfaces;

public interface IEntidadActivable
{
    bool IsActive { get; set; }
}
```

### 0.2 EntidadBase (verificar que implementa IEntidadActivable)
**Ubicación**: `src/BussinesMS.Dominio/Entidades/Compartido/EntidadBase.cs`
```csharp
using BussinesMS.Dominio.Interfaces;

namespace BussinesMS.Dominio.Entidades.Compartido;

public abstract class EntidadBase : IEntidadActivable
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int CreatedByUsuarioId { get; set; }
    public int? UpdatedByUsuarioId { get; set; }
    public int? DeletedByUsuarioId { get; set; }
}
```

### 0.3 ValidacionEntidad (verificar que usa constraint tipado, no reflexión)
**Ubicación**: `src/BussinesMS.Aplicacion/Helpers/ValidacionEntidad.cs`
```csharp
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Excepciones;
using BussinesMS.Dominio.Interfaces;

namespace BussinesMS.Aplicacion.Helpers;

public static class ValidacionEntidad
{
    // Verifica que la entidad existe (no null)
    public static void VerificarExiste<T>(T? entidad, string nombre) where T : class
    {
        if (entidad == null)
            throw new EntidadNoEncontradaException(nombre, 0);
    }

    // Verifica que la entidad existe Y está activa — SIN reflexión
    public static void VerificarActivo<T>(T? entidad, string nombre)
        where T : class, IEntidadActivable
    {
        if (entidad == null)
            throw new EntidadNoEncontradaException(nombre, 0);

        if (!entidad.IsActive)
            throw new ValidacionException($"{nombre} no está activo");
    }

    // Verifica que no existe duplicado
    public static void VerificarNoDuplicado(bool existe, string nombreEntidad, string nombre)
    {
        if (existe)
            throw new EntidadDuplicadaException(nombreEntidad, nombre);
    }

    // Específico para categorías jerárquicas
    public static void VerificarCategoriaRaiz(Categoria? categoria, int? id)
    {
        if (categoria == null)
            throw new EntidadNoEncontradaException("Categoría", id ?? 0);

        if (categoria.ParentId != null)
            throw new CategoriaInvalidaException(
                "No se puede crear subcategoría de una subcategoría. Seleccione una categoría raíz.");
    }
}
```

### 0.4 BaseController (verificar que tiene RespuestaCreado)
**Ubicación**: `src/BussinesMS.API/Controllers/BaseController.cs`
```csharp
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult RespuestaOk<T>(T datos, string mensaje = "Operación exitosa")
    {
        return Ok(new { Success = true, Message = mensaje, Data = datos });
    }

    // ✅ NUEVO — para respuestas 201 Created
    protected IActionResult RespuestaCreado<T>(T datos, string mensaje = "Recurso creado exitosamente")
    {
        return StatusCode(201, new { Success = true, Message = mensaje, Data = datos });
    }

    protected IActionResult RespuestaError(string mensaje, int statusCode = 400)
    {
        return StatusCode(statusCode, new { Success = false, Message = mensaje });
    }
}
```

### 0.5 RepositorioBase (verificar soft-delete correcto)
**Ubicación**: `src/BussinesMS.Infraestructura/Repositorios/Compartido/RepositorioBase.cs`
```csharp
using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Compartido;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Compartido;

public class RepositorioBase<T> : IRepositorio<T> where T : EntidadBase
{
    protected readonly DbContext _contexto;
    protected readonly DbSet<T> _dbSet;
    protected readonly ICurrentUserService _currentUser;

    public RepositorioBase(DbContext contexto, ICurrentUserService currentUser)
    {
        _contexto = contexto;
        _dbSet = contexto.Set<T>();
        _currentUser = currentUser;
    }

    public virtual IQueryable<T> AsQueryable() => _dbSet.AsQueryable();

    public virtual async Task<List<T>> ObtenerTodosAsync() => await _dbSet.ToListAsync();

    public virtual async Task<T?> ObtenerPorIdAsync(int id) => await _dbSet.FindAsync(id);

    public virtual async Task<T> CrearAsync(T entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        await _dbSet.AddAsync(entidad);
        await _contexto.SaveChangesAsync();
        return entidad;
    }

    public virtual async Task<T> ActualizarAsync(T entidad)
    {
        // Usuario sistema (1) como fallback — nunca delete físico
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entidad);
        await _contexto.SaveChangesAsync();
        return entidad;
    }

    public virtual async Task EliminarAsync(int id)
    {
        var entidad = await _dbSet.FindAsync(id);
        if (entidad == null) return;

        // ✅ Siempre soft-delete — nunca delete físico
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _dbSet.Update(entidad);
        await _contexto.SaveChangesAsync();
    }
}
```

---

## Paso 1 — Enum de filtro (si el módulo lo necesita)

Si el módulo tiene filtros de tipo/estado específicos, crear el enum en Dominio.

**Ubicación**: `src/BussinesMS.Dominio/Enums/[NombreEnum].cs`
```csharp
namespace BussinesMS.Dominio.Enums;

public enum [NombreEnum]
{
    Todos = 0,
    Tipo1 = 1,
    Tipo2 = 2
}
```

Luego referenciar desde `GenericPaginationQueryDto` si aplica.

---

## Paso 2 — Entidad de Dominio

**Ubicación**: `src/BussinesMS.Dominio/Entidades/[Sistema]/[NombreEntidad].cs`

```csharp
using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.[Sistema];

public class [NombreEntidad] : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    // propiedades específicas del módulo
}
```

---

## Paso 3 — DTOs

**Ubicación**: `src/BussinesMS.Aplicacion/DTOs/[Sistema]/[NombreEntidad]Dto.cs`

Campos de auditoría en el DTO de lectura: SOLO `Id`, campo principal, `IsActive`, `CreatedAt`.
NO incluir `UpdatedAt`, `DeletedAt`, `*ByUsuarioId` a menos que se pidan explícitamente.

```csharp
namespace BussinesMS.Aplicacion.DTOs.[Sistema];

public class [NombreEntidad]Dto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    // propiedades de lectura
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Crear[NombreEntidad]Dto
{
    public string Nombre { get; set; } = string.Empty;
    // propiedades requeridas para crear (sin Id)
}

public class Actualizar[NombreEntidad]Dto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    // propiedades actualizables
    public bool IsActive { get; set; }
}
```

---

## Paso 4 — Interfaz Repositorio

**Ubicación**: `src/BussinesMS.Aplicacion/Interfaces/[Sistema]/I[NombreEntidad]Repository.cs`

⚠️ TODOS los métodos retornan entidades de dominio, NUNCA DTOs.

```csharp
using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.[Sistema];

namespace BussinesMS.Aplicacion.Interfaces.[Sistema];

public interface I[NombreEntidad]Repository : IRepositorio<[NombreEntidad]>
{
    // ✅ Retorna entidad, no DTO
    Task<[NombreEntidad]?> ObtenerConDetallesAsync(int id);

    // ✅ Retorna lista de entidades, no DTOs
    Task<List<[NombreEntidad]>> ObtenerActivosAsync();

    // Validación de duplicados
    Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null);
}
```

---

## Paso 5 — Interfaz Service

**Ubicación**: `src/BussinesMS.Aplicacion/Interfaces/[Sistema]/I[NombreEntidad]Service.cs`

El Service sí retorna DTOs — es la capa que hace el mapeo.

```csharp
using BussinesMS.Aplicacion.DTOs.[Sistema];
using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.Interfaces.[Sistema];

public interface I[NombreEntidad]Service
{
    Task<PagedResultDto<[NombreEntidad]Dto>> ObtenerTodosAsync(GenericPaginationQueryDto query);
    Task<[NombreEntidad]Dto?> ObtenerPorIdAsync(int id);
    Task<[NombreEntidad]Dto> CrearAsync(Crear[NombreEntidad]Dto dto);
    Task<[NombreEntidad]Dto> ActualizarAsync(Actualizar[NombreEntidad]Dto dto);
    Task EliminarAsync(int id);
}
```

---

## Paso 6 — Implementación Service

**Ubicación**: `src/BussinesMS.Aplicacion/Servicios/[Sistema]/[NombreEntidad]Service.cs`

Reglas:
- Inyecta repositorios, NUNCA DbContext
- Usa `_mapper.Map<>` para TODOS los retornos que sean mapeo 1:1 de campos (no mapeo manual sin motivo)
- Si el DTO expone algún campo `DateTime` (`CreatedAt`, etc.), conviértelo SIEMPRE a hora de Bolivia
  con `BoliviaTimeZone.ToLocal(...)` después de mapear (ver REGLA 3 arriba) — `_mapper.Map<>` solo
  copia el valor crudo en UTC
- Usa excepciones de dominio tipadas (no `throw new Exception`)
- try-catch con logging en cada método público

```csharp
using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common; // BoliviaTimeZone
using BussinesMS.Aplicacion.DTOs.[Sistema];
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.[Sistema];
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.[Sistema];

public class [NombreEntidad]Service : I[NombreEntidad]Service
{
    private readonly I[NombreEntidad]Repository _repo;
    private readonly IMapper _mapper;
    private readonly ILogger<[NombreEntidad]Service> _logger;

    public [NombreEntidad]Service(
        I[NombreEntidad]Repository repo,
        IMapper mapper,
        ILogger<[NombreEntidad]Service> logger)
    {
        _repo = repo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<[NombreEntidad]Dto>> ObtenerTodosAsync(GenericPaginationQueryDto query)
    {
        try
        {
            var baseQuery = _repo.AsQueryable().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x => x.Nombre.ToLower().Contains(f));
            }

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);
            var entidades = await filteredQuery.ToListAsync();

            // ✅ Mapper para el retorno + conversión de fecha a hora de Bolivia
            var items = _mapper.Map<List<[NombreEntidad]Dto>>(entidades);
            for (int i = 0; i < items.Count; i++)
                items[i].CreatedAt = BoliviaTimeZone.ToLocal(entidades[i].CreatedAt);

            return new PagedResultDto<[NombreEntidad]Dto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener [NombreEntidad]s");
            throw;
        }
    }

    public async Task<[NombreEntidad]Dto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            // ✅ Mapper + conversión de fecha a hora de Bolivia
            var dto = _mapper.Map<[NombreEntidad]Dto>(entidad);
            dto.CreatedAt = BoliviaTimeZone.ToLocal(entidad.CreatedAt);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener [NombreEntidad] {Id}", id);
            throw;
        }
    }

    public async Task<[NombreEntidad]Dto> CrearAsync(Crear[NombreEntidad]Dto dto)
    {
        try
        {
            // Validar duplicado
            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteNombreAsync(dto.Nombre),
                "[NombreEntidad]", dto.Nombre);

            var entidad = _mapper.Map<[NombreEntidad]>(dto);
            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("[NombreEntidad] creada: {Nombre}", creada.Nombre);

            // ✅ Mapper para retorno + conversión de fecha a hora de Bolivia
            var resultado = _mapper.Map<[NombreEntidad]Dto>(creada);
            resultado.CreatedAt = BoliviaTimeZone.ToLocal(creada.CreatedAt);
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear [NombreEntidad]");
            throw;
        }
    }

    public async Task<[NombreEntidad]Dto> ActualizarAsync(Actualizar[NombreEntidad]Dto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);

            // ✅ VerificarActivo con constraint tipado (no reflexión)
            ValidacionEntidad.VerificarActivo(existente, "[NombreEntidad]");

            ValidacionEntidad.VerificarNoDuplicado(
                await _repo.ExisteNombreAsync(dto.Nombre, dto.Id),
                "[NombreEntidad]", dto.Nombre);

            existente!.Nombre = dto.Nombre;
            // actualizar otras propiedades

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("[NombreEntidad] actualizada: {Nombre}", actualizada.Nombre);

            // ✅ Mapper para retorno
            return _mapper.Map<[NombreEntidad]Dto>(actualizada);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar [NombreEntidad] {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            ValidacionEntidad.VerificarActivo(existente, "[NombreEntidad]");
            await _repo.EliminarAsync(id);
            _logger.LogInformation("[NombreEntidad] eliminada: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar [NombreEntidad] {Id}", id);
            throw;
        }
    }
```

---

## Paso 7 — Implementación Repositorio

**Ubicación**: `src/BussinesMS.Infraestructura/Repositorios/[Sistema]/[NombreEntidad]Repository.cs`

Reglas:
- Inyecta DbContext específico (SistemaDbContext, AuthDbContext, etc.)
- Inyecta ICurrentUserService para auditoría
- TODOS los métodos retornan entidades, NUNCA DTOs
- La auditoría de CreatedAt/UpdatedAt puede delegarse al RepositorioBase si hereda de él

```csharp
using BussinesMS.Aplicacion.Interfaces.[Sistema];
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.[Sistema];
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.[Sistema];

public class [NombreEntidad]Repository : I[NombreEntidad]Repository
{
    private readonly [Nombre]DbContext _context;
    private readonly ICurrentUserService _currentUser;

    public [NombreEntidad]Repository([Nombre]DbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<[NombreEntidad]> AsQueryable()
        => _context.[NombreEntidad]s.AsQueryable();

    public async Task<List<[NombreEntidad]>> ObtenerTodosAsync()
        => await _context.[NombreEntidad]s
            .Where(x => x.IsActive)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

    public async Task<[NombreEntidad]?> ObtenerPorIdAsync(int id)
        => await _context.[NombreEntidad]s
            .FirstOrDefaultAsync(x => x.Id == id);

    // ✅ Retorna entidad, no DTO
    public async Task<[NombreEntidad]?> ObtenerConDetallesAsync(int id)
        => await _context.[NombreEntidad]s
            .Include(x => x.RelacionNavegacion)   // si aplica
            .FirstOrDefaultAsync(x => x.Id == id);

    // ✅ Retorna lista de entidades
    public async Task<List<[NombreEntidad]>> ObtenerActivosAsync()
        => await _context.[NombreEntidad]s
            .Where(x => x.IsActive)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

    public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null)
    {
        var query = _context.[NombreEntidad]s
            .Where(x => x.Nombre.ToLower() == nombre.ToLower() && x.IsActive);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<[NombreEntidad]> CrearAsync([NombreEntidad] entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.[NombreEntidad]s.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<[NombreEntidad]> ActualizarAsync([NombreEntidad] entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.[NombreEntidad]s.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.[NombreEntidad]s.FindAsync(id);
        if (entidad == null) return;

        // ✅ Siempre soft-delete — nunca delete físico
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.[NombreEntidad]s.Update(entidad);
        await _context.SaveChangesAsync();
    }
}
```

---

## Paso 8 — Controller

**Ubicación**: `src/BussinesMS.API/Controllers/[NombreEntidad]sController.cs`

Ruta según sistema:
- `[Route("api/Sistema/[controller]")]` para módulos de Sistema
- `[Route("api/Auth/[controller]")]` para módulos de Auth
- `[Route("api/Navidad/[controller]")]` para módulos de Navidad

```csharp
using BussinesMS.Aplicacion.DTOs.[Sistema];
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.[Sistema];
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/[Sistema]/[controller]")]
[Produces("application/json")]
public class [NombreEntidad]sController : BaseController
{
    private readonly I[NombreEntidad]Service _servicio;

    public [NombreEntidad]sController(I[NombreEntidad]Service servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GenericPaginationQueryDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("[NombreEntidad] no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Crear[NombreEntidad]Dto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        // ✅ RespuestaCreado desde BaseController — no StatusCode(201) inline
        return RespuestaCreado(resultado, "[NombreEntidad] creada");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] Actualizar[NombreEntidad]Dto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "[NombreEntidad] actualizada exitosamente.");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "[NombreEntidad] eliminada" });
    }
}
```

---

## Paso 9 — DbContext: agregar DbSet y configuración EF

**Ubicación**: `src/BussinesMS.Infraestructura/Persistencia/DbContexts.cs`

Agregar en el DbContext correspondiente:

```csharp
// En [Nombre]DbContext:
public DbSet<[NombreEntidad]> [NombreEntidad]s => Set<[NombreEntidad]>();

// En OnModelCreating:
modelBuilder.Entity<[NombreEntidad]>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
    entity.HasIndex(e => e.Nombre).IsUnique();  // si aplica
    // relaciones si aplica
});
```

---

## Paso 10 — AutoMapper: agregar mapeos

**Ubicación**: `src/BussinesMS.Aplicacion/Mapeos/MappingProfile.cs`

Agregar dentro del constructor de `MappingProfile`:

```csharp
// [NombreEntidad]
CreateMap<[NombreEntidad], [NombreEntidad]Dto>();
CreateMap<[NombreEntidad]Dto, [NombreEntidad]>();
CreateMap<Crear[NombreEntidad]Dto, [NombreEntidad]>();
CreateMap<Actualizar[NombreEntidad]Dto, [NombreEntidad]>();
```

Si hay propiedades de navegación en el DTO (ej: NombrePadre), configurar la proyección:
```csharp
CreateMap<[NombreEntidad], [NombreEntidad]Dto>()
    .ForMember(dest => dest.NombreRelacion,
               opt => opt.MapFrom(src => src.Relacion != null ? src.Relacion.Nombre : null));
```

---

## Paso 11 — Registro en Program.cs

```csharp
// Repositorios
builder.Services.AddScoped<
    BussinesMS.Aplicacion.Interfaces.[Sistema].I[NombreEntidad]Repository,
    BussinesMS.Infraestructura.Repositorios.[Sistema].[NombreEntidad]Repository>();

// Servicios
builder.Services.AddScoped<
    BussinesMS.Aplicacion.Interfaces.[Sistema].I[NombreEntidad]Service,
    BussinesMS.Aplicacion.Servicios.[Sistema].[NombreEntidad]Service>();
```

---

## Paso 12 — Verificar build

```bash
dotnet build BussinesMS.sln
```

Si hay errores de compilación, corregirlos antes de continuar. No generar migración con build fallido.

---

## Paso 13 — Migración EF

```bash
# Crear migración
dotnet ef migrations add Add[NombreEntidad] \
  -p src/BussinesMS.Infraestructura \
  -s src/BussinesMS.API \
  --context [Nombre]DbContext

# Aplicar a la BD
dotnet ef database update \
  -p src/BussinesMS.Infraestructura \
  -s src/BussinesMS.API \
  --context [Nombre]DbContext
```

---

## Paso 14 — Preguntar sobre migración de datos (OBLIGATORIO)

Después de build exitoso y migración aplicada, preguntar al usuario:

```
"¿Querés agregar [NombreEntidad] al sistema de migraciones de datos?
Esto permitirá cargar registros desde el BDMS.csv via POST api/Sistema/Migraciones"
```

- Si responde SÍ → usar skill `crear-migracion`
- Si responde NO → documentar y continuar

---

## Resumen de orden de ejecución

| # | Acción |
|---|--------|
| 0 | Verificar/corregir archivos compartidos (IEntidadActivable, ValidacionEntidad, RepositorioBase, BaseController) |
| 1 | Crear enum de dominio si el módulo lo necesita |
| 2 | Crear entidad en Dominio |
| 3 | Crear DTOs en Aplicación |
| 4 | Crear interfaz repositorio (retorna entidades, no DTOs) |
| 5 | Crear interfaz service (retorna DTOs) |
| 6 | Implementar service (mapper en todos los retornos, excepciones tipadas) |
| 7 | Implementar repositorio (retorna entidades, soft-delete siempre) |
| 8 | Crear controller (RespuestaCreado para POST) |
| 9 | Agregar DbSet y config EF en DbContext |
| 10 | Agregar mapeos en MappingProfile |
| 11 | Registrar en Program.cs |
| 12 | `dotnet build` — verificar que compila |
| 13 | Generar y aplicar migración EF |
| 14 | Preguntar al usuario sobre migración de datos |