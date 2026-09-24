# BussinesMS - AGENTS.md

# A.R.I.S. - Sistema de Automatización

## 🤖 A.R.I.S. - Automated Resource & Inventory System

¡Bienvenido! Soy A.R.I.S., tu asistente automatizado para el desarrollo de BussinesMS.

### Detección Automática de Tareas

A.R.I.S. detecta automáticamente qué tipo de tarea vas a realizar y decide si usar subagentes o el agente principal.

### Tareas que van a SUBAGENTES (Automático):

| Palabras Clave | Descripción |
|----------------|-------------|
| crear, agregar, nuevo módulo | **Delegar a subagente con skill** - Cargar skill apropiada y lanzar subagente |
| crear entidad | Delegar a subagente con skill crear-entidad |
| crear servicio | Delegar a subagente con skill crear-servicio |
| crear repositorio | Delegar a subagente (repositorio ya existe como parte de módulo) |
| crear controller | Delegar a subagente (controller ya existe como parte de módulo) |
| crear DTO | Delegar a subagente (DTOs ya existen como parte de módulo) |
| implementar módulo | Delegar a subagente con skill apropiada |

### Tareas que hace el AGENTE PRINCIPAL:

| Palabras Clave | Descripción |
|----------------|-------------|
| actualizar skill | Modificar archivos de skills (crear/editar skills) |
| modificar arquitectura | Cambiar estructura del proyecto |
| corregir error | Debug y soluciones importantes |
| mejorar patrón | Refactorización significativa |
| actualizar guía | Modificar AGENTS.md o BIENVENIDA.md |
| actualizar mapeo | Modificar AutoMapper |
| revisar código | Análisis profundo |
| explicar | Necesita contexto completo |

### Cómo funciona la detección:

1. Cuando escribes, analizo las palabras clave
2. Si es tarea repetitiva de creación → uso subagente automáticamente
3. Si es tarea importante/modificación → lo hago yo mismo
4. Si es información → respondo directamente

---

### Lista de Módulos Actuales

| Sistema | Módulo | Fecha Creación | Estado |
|---------|--------|----------------|--------|
| Auth | Sistema | - | ✅ |
| Auth | Rol | - | ✅ |
| Auth | Usuario | - | ✅ |
| Auth | Almacen | - | ✅ |
| Auth | Menu | - | ✅ |
| Auth | Permiso | - | ✅ |
| Sistema | Categoria | 14/04/2026 | ✅ |
| Sistema | Fabricante | 14/04/2026 | ✅ |
| Sistema | DescripcionSabor | 14/04/2026 | ✅ |
| Sistema | DescripcionTamanio | 14/04/2026 | ✅ |
| Sistema | TipoPresentacion | 17/04/2026 | ✅ |
| Sistema | Producto | 03/05/2026 | ✅ |
| Sistema | ProductoVariante | 03/05/2026 | ✅ |
| Sistema | Proveedor | 27/05/2026 | ✅ |
| Sistema | Compra | 28/05/2026 | ✅ |
| Sistema | PagoCompra | - | ✅ |
| Sistema | InventarioLote | - | ✅ |
| Sistema | InventarioLoteAlmacen | - | ✅ |
| Sistema | MovimientoInventario | - | ✅ |
| Sistema | Traslado (vía MovimientoInventario) | - | ✅ |
| Sistema | DevolucionCliente | - | ✅ |
| Sistema | SesionCaja | - | ✅ |
| Sistema | Venta | - | 🟡 funcional, falta confirmar/ajustar endpoint de productos para el POS (ver MEMORIA.md) |
| Sistema | VentaDetalle | - | ✅ |
| Sistema | CategoriaGasto / GastoOperativo | - | ✅ |

> Auditado contra el código real el 22/09/2026. Ver `docs/ERD.md` para el detalle de esquema y `../MEMORIA.md` (raíz del proyecto) para deuda técnica pendiente.

---

## Build Commands

```bash
# Build entire solution
dotnet build BussinesMS.sln

# Build specific project
dotnet build src/BussinesMS.API/BussinesMS.API.csproj
dotnet build src/BussinesMS.Aplicacion/BussinesMS.Aplicacion.csproj
dotnet build src/BussinesMS.Infraestructura/BussinesMS.Infraestructura.csproj
dotnet build src/BussinesMS.Dominio/BussinesMS.Dominio.csproj

# Run API
dotnet run --project src/BussinesMS.API

# Run specific project
dotnet run --project src/BussinesMS.Dominio

# Restore packages
dotnet restore BussinesMS.sln

# Run tests (when implemented)
dotnet test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

## Project Overview

- **Framework**: .NET 9
- **ORM**: Entity Framework Core 9 (SQL Server)
- **Architecture**: Clean Architecture (4 layers)
- **Validation**: FluentValidation
- **Mapping**: AutoMapper
- **Logging**: Serilog
- **Documentation**: Swagger (available at /swagger)

## Code Style Guidelines

### Architecture Structure

```
src/
├── BussinesMS.Dominio/           # Core business logic - NO external dependencies
│   ├── Entidades/                 # Domain entities (inherit from EntidadBase)
│   ├── Interfaces/               # Repository contracts
│   └── Excepciones/              # Domain exceptions
│
├── BussinesMS.Aplicacion/        # Use cases and business services
│   ├── DTOs/                     # Data transfer objects
│   ├── Interfaces/               # Service contracts
│   ├── Mapeos/                   # AutoMapper profiles
│   └── Validaciones/             # FluentValidation validators
│
├── BussinesMS.Infraestructura/   # External implementations
│   ├── Persistencia/             # EF Core DbContext and configurations
│   └── Repositorios/             # Repository implementations
│
└── BussinesMS.API/               # Presentation layer
    ├── Controllers/              # REST API controllers
    ├── Middlewares/              # Custom middleware
    ├── Filtros/                  # API filters
    └── Configuracion/            # Settings
```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Entities | PascalCase + Entity suffix | `Producto`, `Categoria`, `Inventario` |
| DTOs | PascalCase + Dto suffix | `ProductoDto`, `CrearProductoDto` |
| Services | PascalCase + Servicio prefix | `ServicioProducto`, `ServicioInventario` |
| Repositories | PascalCase + Repositorio prefix | `RepositorioProducto` |
| Controllers | PascalCase + Controller suffix | `ProductosController` |
| Interfaces | I + PascalCase | `IRepositorioProducto`, `IServicioProducto` |
| Properties | PascalCase | `NombreProducto`, `Cantidad` |
| Private fields | underscore + camelCase | `_contexto`, `_mapper` |

### File Organization

- **One class per file**: Each file contains only one public class
- **File naming**: Same as class name + .cs extension
- **Using statements**: Organized alphabetically at top of file
- **Namespaces**: File-scoped (`namespace BussinesMS.Dominio.Entidades;`)

### Code Patterns

#### Entity Base Pattern (with Audit)
```csharp
namespace BussinesMS.Dominio.Entidades;

public abstract class EntidadBase
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

public class Producto : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}
```

#### Service Pattern (with Logging)
```csharp
namespace BussinesMS.Aplicacion.Servicios;

public class ServicioProducto : IServicioProducto
{
    private readonly IRepositorio<Producto> _repositorio;
    private readonly IMapper _mapper;
    private readonly ILogger<ServicioProducto> _logger;

    public ServicioProducto(IRepositorio<Producto> repositorio, IMapper mapper, ILogger<ServicioProducto> logger)
    {
        _repositorio = repositorio;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var producto = await _repositorio.ObtenerPorIdAsync(id);
            return producto == null ? null : _mapper.Map<ProductoDto>(producto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener producto {Id}", id);
            throw;
        }
    }
}
```

### AutoMapper Configuration
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // PARA CADA ENTIDAD, AGREGAR:
        CreateMap<Producto, ProductoDto>();
        CreateMap<ProductoDto, Producto>();
        CreateMap<CrearProductoDto, Producto>();
    }
}
```

#### Controller Pattern
```csharp
namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductosController : BaseController
{
    private readonly IServicioProducto _servicio;

    public ProductosController(IServicioProducto servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null ? RespuestaError("Producto no encontrado", 404) : RespuestaOk(resultado);
    }
}
```

### Advanced Patterns (from SistemaVentas)

#### Generic Repository Interface
```csharp
namespace BussinesMS.Aplicacion.Interfaces;

public interface IRepositorio<T> where T : class
{
    Task<List<T>> ObtenerTodosAsync();
    Task<T?> ObtenerPorIdAsync(int id);
    Task<T> CrearAsync(T entidad);
    Task<T> ActualizarAsync(T entidad);
    Task EliminarAsync(int id);
}
```

#### Specific Repository Interface (extends generic)
```csharp
public interface IVentaRepository : IRepositorio<Venta>
{
    Task<List<VentaListDto>> ObtenerTodosAsync();
    Task<Venta?> ObtenerConDetallesAsync(int id);
}
```

#### Repository Implementation (knows DbContext)
```csharp
public class VentaRepository : IVentaRepository
{
    private readonly AppDbContext _context;

    public VentaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Venta> CrearAsync(Venta venta)
    {
        _context.Ventas.Add(venta);
        await _context.SaveChangesAsync();
        return venta;
    }
}
```

#### Service Interface (specific)
```csharp
public interface IVentaService 
{
    Task<int> ProcesarVentaAsync(CrearVentaDto dto);
    Task<VentaResponseDto?> ObtenerPorIdAsync(int id);
    Task<List<VentaListDto>> ObtenerTodosAsync();
    Task AnularVentaAsync(int id, string motivo);
}
```

#### Service Implementation (INJECT repositories, NOT DbContext)
```csharp
public class VentaService : IVentaService
{
    private readonly IVentaRepository _repo;
    private readonly IDetalleVentaRepository _detalleRepo;
    private readonly IClienteRepository _clienteRepo;
    private readonly IUnitOfWork _uow;

    public VentaService(
        IVentaRepository repo,
        IDetalleVentaRepository detalleRepo,
        IClienteRepository clienteRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _detalleRepo = detalleRepo;
        _clienteRepo = clienteRepo;
        _uow = uow;
    }

    public async Task<int> ProcesarVentaAsync(CrearVentaDto dto)
    {
        // Service knows NOTHING about DbContext
        // Only uses repositories
        var cliente = await _clienteRepo.ObtenerPorIdAsync(dto.ClienteId);
        // ... business logic
    }
}
```

#### UnitOfWork Pattern (for transactions)
```csharp
public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

// Usage in service:
await _uow.BeginTransactionAsync();
try
{
    await _repo.CrearAsync(venta);
    await _detalleRepo.CrearAsync(detalle);
    await _uow.CommitAsync();
}
catch
{
    await _uow.RollbackAsync();
    throw;
}
```

#### Validation Helper
```csharp
public static class ValidacionEntidad
{
    public static void VerificarActivo<T>(T? entidad, string nombre) where T : class
    {
        if (entidad == null)
            throw new Exception($"{nombre} no existe");
        
        var propiedad = entidad.GetType().GetProperty("Activo");
        if (propiedad?.GetValue(entidad) is false)
            throw new Exception($"{nombre} no está activo");
    }
}
```

### Error Handling

- Use domain exceptions in BussinesMS.Dominio.Excepciones
- Return standardized responses via BaseController methods
- HTTP status codes: 200 (OK), 201 (Created), 400 (Bad Request), 404 (Not Found), 500 (Error)

### Validation Rules

- Use FluentValidation in BussinesMS.Aplicacion.Validaciones
- Register validators in Program.cs with `AddValidatorsFromAssemblyContaining<Program>()`
- Validate on create/update operations in service layer

### DTOs - Campos de Auditoría

**⚠️ IMPORTANTE**: Los DTOs de lectura deben incluir SOLO lo esencial:

| Campo | Incluir en DTOs | Razón |
|-------|-----------------|-------|
| `Id` | ✅ SIEMPRE | Identificador |
| `Nombre` (campo principal) | ✅ SIEMPRE | Dato principal |
| `IsActive` / `Activo` | ✅ SIEMPRE | Estado actual |
| `CreatedAt` | ✅ SIEMPRE | Fecha de creación |
| `UpdatedAt` | ❌ NO (por defecto) | Agregar solo si se necesita |
| `DeletedAt` | ❌ NO (por defecto) | Agregar solo si se necesita |
| `CreatedByUsuarioId` | ❌ NO (por defecto) | Agregar solo si se necesita |
| `UpdatedByUsuarioId` | ❌ NO (por defecto) | Agregar solo si se necesita |
| `DeletedByUsuarioId` | ❌ NO (por defecto) | Agregar solo si se necesita |

**Ejemplo correcto de DTO**:
```csharp
public class ProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public bool Activo { get; set; }
    public DateTime CreatedAt { get; set; }
    // NO incluir UpdatedAt, DeletedByUsuarioId, etc.
}
```

Si necesitas campos de auditoría adicionales, agregarlos manualmente al DTO.

### Dependency Rule

**IMPORTANT**: 
- Services should NEVER know about DbContext
- Only Repositories know about DbContext
- Services inject repositories via interfaces
- Use UnitOfWork for transactions that involve multiple repositories

## Multi-Database Configuration

The system uses 3 separate databases:

| Database | Status | Purpose |
|----------|--------|---------|
| BussinesMS_Auth | ✅ Completo | Authentication, Users, Roles, Almacenes |
| BussinesMS_Sistema | ✅ Completo | Core business (inventory, sales, purchases) |
| BussinesMS_Navidad | ⏳ Pendiente | Holiday season (Oct-Dec) - En planificación |

Each database has its own DbContext in BussinesMS.Infraestructura.Persistencia:
- `AuthDbContext` - Authentication context
- `SistemaDbContext` - Main system context
- `NavidadDbContext` - Holiday system context (pendiente)

## Modelo Entidad-Relación

> **Nota**: El modelo de datos completo está documentado en `docs/ERD.md`. 
> Ese archivo contiene el esquema detallado de todas las tablas y relaciones.
> Este documento es un resumen rápido.

### DB_Auth (Completo)

| Tabla | Descripción |
|-------|-------------|
| Sistema | 1: Regular, 2: Navideño |
| Rol | Admin, VendedorTienda, VendedorRuta, EncargadoAlmacen, Contador |
| Usuario | Usuarios del sistema con JWT |
| Almacen | 3 almacenes (Tienda Principal, Almacén 1, Almacén 2) |

### DB_Sistema (Completo) - Módulo Catálogo

| Tabla | Descripción |
|-------|-------------|
| Categoria | Categorías y subcategorías (jerárquico) |
| Fabricante | Fabricantes de productos |
| DescripcionSabor | Sabores disponibles |
| DescripcionTamanio | Tamaños (100g, 500ml, 1kg) |
| TipoPresentacion | Unidad, Display, Caja, Paquete |
| Producto | Producto maestro |
| ProductoVariante | Variante (sabor + tamaño) |
| ProductoPresentacion | Equivalencias por presentación |
| HistorialPrecio | Histórico de cambios de precio |

### DB_Sistema (Completo) - Módulo Proveedores

| Tabla | Descripción |
|-------|-------------|
| Proveedor | Proveedores del sistema |

### DB_Sistema (Completo) - Módulo Inventario

| Tabla | Descripción |
|-------|-------------|
| InventarioLote | Lotes por fecha de vencimiento (FIFO) |
| MovimientoInventario | Auditoría de movimientos de stock |
| Traslado | Traslados entre almacenes |

### DB_Sistema (Completo) - Módulo Compras

| Tabla | Descripción |
|-------|-------------|
| Compra | Orden de compra |
| CompraDetalle | Detalle de productos comprados |
| PagoCompra | Pagos parciales a proveedores |

### DB_Sistema (Completo) - Módulo Ventas y Caja

| Tabla | Descripción |
|-------|-------------|
| SesionCaja | Sesión de caja por usuario |
| Venta | Venta en tienda |
| VentaDetalle | Detalle con lote específico (FIFO) |
| CategoriaGasto | Categorías de gasto |
| GastoOperativo | Gastos operativos y pagos a proveedores |

### DB_Navidad (Pendiente - En Planificación)

Las siguientes entidades están en planificación:

```
- CodigoProveedor (pertenece a proveedor: Sandra, Crispin, etc.)
- PedidoNavideño (pedido inicial por código)
- EntregaNavideña (entregas parciales por código)
- VentaRuta (sacado - devuelto = vendido)
- GastoVentaRuta (combustible, comida, peajes por día)
- Inversionista (capital + interés % fijo)
- GastosNavideños (gastos específicos de temporada)
```

## 📦 Datos Iniciales - BDMS.csv

El sistema cuenta con un archivo CSV de datos iniciales en:
`src/BussinesMS.API/Configuracion/Data/BDMS.csv`

**Estructura del CSV (601 productos):**

| Columna CSV | Campo BDMS | Entidad |
|-------------|------------|---------|
| codigo de barras | CodigoBarras | Producto (barcode) |
| categoria | - | Categoria (jerárquico) |
| subcategoria | - | Categoria (hijo de categoria) |
| nombre producto | Nombre | Producto |
| sabor o descripcion | - | DescripcionSabor |
| peso tamanio | - | DescripcionTamanio |
| fabrica | Nombre | Fabricante |
| unidad | - | TipoPresentacion (Unidad=1) |
| displey | - | TipoPresentacion (Display) |
| caja | - | TipoPresentacion (Caja) |

**Categorías únicas del CSV (60+ categorías):**
- GALLETAS (CRACKER, GALLETA, GALLETA RELLENA, WAFER, OBLEA, etc.)
- GRAJEAS, PIPOCAS, SNACKS, QUEQUES, MALVAVISCO
- TOALLA HIGIENICA, MANTEQUILLA, FIDEOS, CONSERVAS, LACTEOS
- CACAOS, CAFES, PAÑALES, MISCELANEA, ASEO PERSONAL
- BEBIDAS, INFUCIONES, SALSAS, LIMPIEZA COCINA, CONDIMENTOS, PAPEL

**Fabricantes únicos del CSV (50+):**
CARSA, SAN GABRIEL, FAGAL, FERRARI, MADISA, NESTLE, MONDELEZ, PIL, COCA COLA, SODOMIN, etc.

**Orden de creación recomendado:**
1. Fabricante ✅ (ya existe)
2. Categoria (jerárquica) - crear desde CSV
3. DescripcionSabor - crear desde CSV
4. DescripcionTamanio - crear desde CSV
5. TipoPresentacion - crear desde CSV (Unidad, Display, Caja)
6. Producto - crear desde CSV
7. ProductoVariante - crear desde CSV (sabor + tamaño)

---

## 🔄 Sistema de Migraciones

### Endpoint

```
POST api/Sistema/Migraciones
Content-Type: multipart/form-data
Body:
  - archivo: BDMS.csv
```

**Sin parámetros adicionales** - el servicio detecta automáticamente qué cargar.

### Cómo Funciona

1. Lee el archivo CSV completo
2. Lista únicos en memoria (HashSet para evitar duplicados)
3. Verifica cada registro en la base de datos
4. **Inserta solo los que NO existen**
5. Omite los duplicados automáticamente

### Reglas (CRÍTICO)

1. **Un solo endpoint** para todas las entidades
2. **Inserta solo lo nuevo** - si re-ejecutás el mismo CSV, no pasa nada (ya existen)
3. **El servicio crece** con cada nuevo módulo
4. **Idempotente** - ejecución segura múltiples veces

### Módulos Actualmente Soportados

| Módulo | Columna CSV | Estado |
|--------|------------|--------|
| Categoria | categoria + subcategoria | ✅ |
| Fabricante | fabrica | ✅ |
| DescripcionSabor | sabor o descripcion | ✅ |
| DescripcionTamanio | peso tamanio | ✅ |
| TipoPresentacion | unidad + displey + caja | ✅ |
| Producto | nombre producto + codigo de barras | ✅ |
| ProductoVariante | combinacion | ⏳ Pendiente |

### Para Agregar un Nuevo Módulo

⚠️ **El agente que creó el módulo es el responsable de:**

1. Cuando termina de crear un módulo, **PREGUNTAR** al usuario:
   ```
   "¿Querés agregar este módulo al sistema de migraciones?"
   ```

2. SI el usuario dice "SI":
   - Usar la skill `crear-migracion`
   - Agregar lógica en MigracionService.cs
   - Agregar la lista correspondiente en ResultadoMigracionDto

3. SI el usuario dice "NO":
   - Documentar que falta agregar
   - Continuar sin cambios

### Más Información

- Skill: `.opencode/skills/crear-migracion/SKILL.md`
- Servicio: `src/BussinesMS.Aplicacion/Servicios/Sistema/MigracionService.cs`
- DTO: `src/BussinesMS.Aplicacion/DTOs/Sistema/Migracion/FilaCsv.cs`

---

## SDD (Spec-Driven Development) Workflow

When user requests a module, follow this process:

1. **Understand the requirement**: Read user's spec description
2. **Identify needed components**: Determine what needs to be created based on spec
3. **Use appropriate skill**: Use the right skill for the task type
4. **Create components**: Use subagents to create each component

### FLUJO DE TRABAJO - Orquestador vs Subagentes

⚠️ **IMPORTANTE**: Cuando el usuario pide crear un módulo, el orquestador debe delegar a un subagente usando la skill correspondiente. NO debe hacer el trabajo directamente.

| Paso | Acción | Responsable |
|------|--------|-------------|
| 1 | Usuario dice "crear módulo X" | Usuario |
| 2 | Yo analizo requisitos y confirmo plan | Yo (orquestador) |
| 3 | **Cargo la skill correspondiente** | Yo (orquestador) |
| 4 | **Lanzo subagente con la skill** | **Subagente** |
| 5 | Verifico build y reporto resultado | Yo |

#### Ejemplo de Prompt para Subagente
```
"Crear módulo CRUD para EntidadProveedor usando el skill crear-modulo-crud.
El módulo pertenece al sistema 'Sistema' (no Auth).
DbContext: SistemaDbContext
Endpoints necesarios: GET todos (paginado), GET por ID, POST crear, PUT actualizar, DELETE.
Entidad tiene: Nombre, Nit, Telefono, IsActive, CreatedAt, etc."
```

### Available Skills

| Skill | Use When |
|-------|----------|
| crear-modulo-crud | Need full CRUD (Create, Read, Update, Delete) |
| crear-modulo-read | Need only query operations (GET) |
| crear-modulo-write | Need only write operations (POST, PUT, DELETE) |
| crear-entidad | Need only domain entity |
| crear-servicio | Need only service layer |

### Using Subagents

For complex tasks, delegate to subagents:

```
For crear-modulo-crud:
  - Subagent 1: Create entity in BussinesMS.Dominio/Entidades/
  - Subagent 2: Create repository in BussinesMS.Infraestructura/Repositorios/
  - Subagent 3: Create service in BussinesMS.Aplicacion/Servicios/
  - Subagent 4: Create DTOs in BussinesMS.Aplicacion/DTOs/
  - Subagent 5: Create controller in BussinesMS.API/Controllers/
  - Subagent 6: Configure EF Core in appropriate DbContext
```

## Swagger Configuration

- URL: `https://localhost:5001/swagger` (or configured port)
- JSON: `https://localhost:5001/swagger/v1/swagger.json`
- Enable in Development environment only

## Dependencies Between Projects

```
BussinesMS.API
    └─> BussinesMS.Aplicacion
    └─> BussinesMS.Infraestructura

BussinesMS.Infraestructura
    └─> BussinesMS.Dominio
    └─> BussinesMS.Aplicacion

BussinesMS.Aplicacion
    └─> BussinesMS.Dominio
```

DO NOT create circular dependencies. Always depend on the layer below.