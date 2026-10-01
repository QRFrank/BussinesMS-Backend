using BussinesMS.Aplicacion.Mapeos;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.API.Middlewares;
using BussinesMS.Dominio.Entidades;
using BussinesMS.Infraestructura.Persistencia;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Cryptography;
using System.Text;
using BussinesMS.Infraestructura.Seed;

Console.WriteLine("=== INICIANDO BUSSINESMS API ===");

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("Configurando Logger...");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

Console.WriteLine("Agregando servicios MVC...");
builder.Services.AddControllers();

// Después de builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://spore-actress-ducktail.ngrok-free.dev"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

Console.WriteLine("Agregando Swagger...");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BussinesMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

Console.WriteLine("Agregando FluentValidation...");
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<BussinesMS.Aplicacion.Validadores.Sistema.CrearClienteDtoValidator>();

Console.WriteLine("Agregando AutoMapper...");
builder.Services.AddAutoMapper(typeof(MappingProfile));

Console.WriteLine("Configurando JWT...");
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

Console.WriteLine("Configurando Authentication JWT...");
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Auth failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            var claims = context.Principal?.Claims;
            var usuarioId = int.Parse(claims?.FirstOrDefault(c => c.Type == "sub")?.Value ?? "0");
            var username = claims?.FirstOrDefault(c => c.Type == "unique_name")?.Value ?? "";
            var rolId = int.Parse(claims?.FirstOrDefault(c => c.Type == "rolId")?.Value ?? "0");

            currentUser.SetUser(usuarioId, username, rolId);
            Console.WriteLine($"User authenticated: {username} (ID: {usuarioId})");
            return Task.CompletedTask;
        }
    };
});

Console.WriteLine("Configurando DbContexts...");
builder.Services.AddDbContext<AuthDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("AuthDB"))
    .LogTo(Console.WriteLine));

builder.Services.AddDbContext<SistemaDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("SistemaDB")));

builder.Services.AddDbContext<NavidadDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("NavidadDB")));

// Repositorios
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.ISistemaRepository, BussinesMS.Infraestructura.Repositorios.Auth.SistemaRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.IRolRepository, BussinesMS.Infraestructura.Repositorios.Auth.RolRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.IAlmacenRepository, BussinesMS.Infraestructura.Repositorios.Auth.AlmacenRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.IUsuarioRepository, BussinesMS.Infraestructura.Repositorios.Auth.UsuarioRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.IMenuRepository, BussinesMS.Infraestructura.Repositorios.Auth.MenuRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ICategoriaRepository, BussinesMS.Infraestructura.Repositorios.Sistema.CategoriaRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IFabricanteRepository, BussinesMS.Infraestructura.Repositorios.Sistema.FabricanteRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IDescripcionSaborRepository, BussinesMS.Infraestructura.Repositorios.Sistema.DescripcionSaborRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IDescripcionTamanioRepository, BussinesMS.Infraestructura.Repositorios.Sistema.DescripcionTamanioRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IProductoRepository, BussinesMS.Infraestructura.Repositorios.Sistema.ProductoRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IProductoVarianteRepository, BussinesMS.Infraestructura.Repositorios.Sistema.ProductoVarianteRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IProveedorRepository, BussinesMS.Infraestructura.Repositorios.Sistema.ProveedorRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IClienteRepository, BussinesMS.Infraestructura.Repositorios.Sistema.ClienteRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ICompraRepository, BussinesMS.Infraestructura.Repositorios.Sistema.CompraRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IPagoCompraRepository, BussinesMS.Infraestructura.Repositorios.Sistema.PagoCompraRepository>();

// UnitOfWork
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ISistemaUnitOfWork, BussinesMS.Infraestructura.Persistencia.SistemaUnitOfWork>();

// Servicios
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.ISistemaService, BussinesMS.Aplicacion.Servicios.Auth.SistemaService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.IRolService, BussinesMS.Aplicacion.Servicios.Auth.RolService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.IAlmacenService, BussinesMS.Aplicacion.Servicios.Auth.AlmacenService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.IUsuarioService, BussinesMS.Aplicacion.Servicios.Auth.UsuarioService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Auth.IMenuService, BussinesMS.Aplicacion.Servicios.Auth.MenuService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ICategoriaService, BussinesMS.Aplicacion.Servicios.Sistema.CategoriaService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IFabricanteService, BussinesMS.Aplicacion.Servicios.Sistema.FabricanteService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IDescripcionSaborService, BussinesMS.Aplicacion.Servicios.Sistema.DescripcionSaborService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IDescripcionTamanioService, BussinesMS.Aplicacion.Servicios.Sistema.DescripcionTamanioService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IMigracionService, BussinesMS.Aplicacion.Servicios.Sistema.MigracionService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IProductoService, BussinesMS.Aplicacion.Servicios.Sistema.ProductoService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IProductoVarianteService, BussinesMS.Aplicacion.Servicios.Sistema.ProductoVarianteService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IProveedorService, BussinesMS.Aplicacion.Servicios.Sistema.ProveedorService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IClienteService, BussinesMS.Aplicacion.Servicios.Sistema.ClienteService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ICompraService, BussinesMS.Aplicacion.Servicios.Sistema.CompraService>();
// Repositorios — agrega después de IProductoVarianteRepository
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ITipoPresentacionRepository, BussinesMS.Infraestructura.Repositorios.Sistema.TipoPresentacionRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IProductoPresentacionRepository, BussinesMS.Infraestructura.Repositorios.Sistema.ProductoPresentacionRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IInventarioLoteRepository, BussinesMS.Infraestructura.Repositorios.Sistema.InventarioLoteRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IInventarioLoteAlmacenRepository, BussinesMS.Infraestructura.Repositorios.Sistema.InventarioLoteAlmacenRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IMovimientoInventarioRepository, BussinesMS.Infraestructura.Repositorios.Sistema.MovimientoInventarioRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IDevolucionClienteRepository, BussinesMS.Infraestructura.Repositorios.Sistema.DevolucionClienteRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ISesionCajaRepository, BussinesMS.Infraestructura.Repositorios.Sistema.SesionCajaRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IVentaRepository, BussinesMS.Infraestructura.Repositorios.Sistema.VentaRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ICategoriaGastoRepository, BussinesMS.Infraestructura.Repositorios.Sistema.CategoriaGastoRepository>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IGastoOperativoRepository, BussinesMS.Infraestructura.Repositorios.Sistema.GastoOperativoRepository>();

// Servicios — agrega después de IProductoVarianteService
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ITipoPresentacionService, BussinesMS.Aplicacion.Servicios.Sistema.TipoPresentacionService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IProductoPresentacionService, BussinesMS.Aplicacion.Servicios.Sistema.ProductoPresentacionService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IInventarioLoteService, BussinesMS.Aplicacion.Servicios.Sistema.InventarioLoteService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IMovimientoInventarioService, BussinesMS.Aplicacion.Servicios.Sistema.MovimientoInventarioService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ITrasladoService, BussinesMS.Aplicacion.Servicios.Sistema.TrasladoService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IDevolucionClienteService, BussinesMS.Aplicacion.Servicios.Sistema.DevolucionClienteService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IVarianteStockService, BussinesMS.Aplicacion.Servicios.Sistema.VarianteStockService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ISesionCajaService, BussinesMS.Aplicacion.Servicios.Sistema.SesionCajaService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IVentaService, BussinesMS.Aplicacion.Servicios.Sistema.VentaService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.ICategoriaGastoService, BussinesMS.Aplicacion.Servicios.Sistema.CategoriaGastoService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IGastoOperativoService, BussinesMS.Aplicacion.Servicios.Sistema.GastoOperativoService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IPagoCompraService, BussinesMS.Aplicacion.Servicios.Sistema.PagoCompraService>();
builder.Services.AddScoped<BussinesMS.Aplicacion.Interfaces.Sistema.IVencimientoLoteService, BussinesMS.Aplicacion.Servicios.Sistema.VencimientoLoteService>();

// Background Services
builder.Services.AddHostedService<BussinesMS.Infraestructura.Jobs.VencimientoLotesJob>();

Console.WriteLine("Construyendo aplicación...");
var app = builder.Build();

// Reemplazar el bloque try/catch del seed en Program.cs
Console.WriteLine("Ejecutando seed data...");
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthSeeder>>();
    var seeder = new AuthSeeder(context, logger);
    await seeder.EjecutarAsync();
    Console.WriteLine("Seed completado!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error en seed: {ex.Message}");
}

// Swagger siempre activo
Console.WriteLine("Activando Swagger...");
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseErrorHandling();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("=== SERVIDOR INICIADO ===");
Console.WriteLine("Swagger: http://localhost:5001/swagger");

var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();