using AutoMapper;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Mapeos;

public class NavidadMappingProfile : Profile
{
    public NavidadMappingProfile()
    {
        // Temporadas
        CreateMap<Temporada, TemporadaDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)))
            .ForMember(dest => dest.EstadoNombre, opt => opt.MapFrom(src => src.Estado.ToString()))
            .ForMember(dest => dest.AlmacenesConConteo, opt => opt.Ignore());
        CreateMap<CrearTemporadaDto, Temporada>()
            .ForMember(dest => dest.AlmacenesConteo, opt => opt.Ignore());
        CreateMap<ActualizarTemporadaDto, Temporada>()
            .ForMember(dest => dest.AlmacenesConteo, opt => opt.Ignore());

        // Inversores
        CreateMap<Inversor, InversorDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<CrearInversorDto, Inversor>();

        // Aportes de capital (InversorId null = capital propio)
        CreateMap<AporteCapital, AporteCapitalDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)))
            .ForMember(dest => dest.EsCapitalPropio, opt => opt.MapFrom(src => src.InversorId == null))
            .ForMember(dest => dest.InversorNombre, opt => opt.MapFrom(src =>
                src.InversorId == null ? "Capital propio" : (src.Inversor != null ? src.Inversor.Nombre : string.Empty)));

        // Pagos a inversores
        CreateMap<PagoInversor, PagoInversorDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)))
            .ForMember(dest => dest.TipoNombre, opt => opt.MapFrom(src => src.Tipo.ToString()))
            .ForMember(dest => dest.InversorId, opt => opt.MapFrom(src => src.AporteCapital != null ? src.AporteCapital.InversorId : null))
            .ForMember(dest => dest.InversorNombre, opt => opt.MapFrom(src =>
                src.AporteCapital != null && src.AporteCapital.Inversor != null ? src.AporteCapital.Inversor.Nombre : string.Empty));

        // Categorías de gasto (globales)
        CreateMap<CategoriaGastoNav, CategoriaGastoNavDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<CrearCategoriaGastoNavDto, CategoriaGastoNav>();

        // Categorías de producto (globales)
        CreateMap<CategoriaProductoNav, CategoriaProductoNavDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<CrearCategoriaProductoNavDto, CategoriaProductoNav>();

        // Gastos de temporada
        CreateMap<GastoNav, GastoNavDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)))
            .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.Nombre : string.Empty));

        // Proveedores de temporada (conteos y códigos los llena el servicio)
        CreateMap<Proveedor, ProveedorNavDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)))
            .ForMember(dest => dest.CantidadCodigos, opt => opt.Ignore())
            .ForMember(dest => dest.CantidadProductos, opt => opt.Ignore())
            .ForMember(dest => dest.Codigos, opt => opt.Ignore());

        // Códigos de cliente
        CreateMap<CodigoCliente, CodigoClienteDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)))
            .ForMember(dest => dest.ProveedorNombre, opt => opt.MapFrom(src => src.Proveedor != null ? src.Proveedor.Nombre : string.Empty));

        // Clientes navideños (globales)
        CreateMap<ClienteNav, ClienteNavDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<CrearClienteNavDto, ClienteNav>();

        // Productos de temporada
        CreateMap<Producto, ProductoNavDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)))
            .ForMember(dest => dest.ProveedorNombre, opt => opt.MapFrom(src => src.Proveedor != null ? src.Proveedor.Nombre : string.Empty))
            .ForMember(dest => dest.CategoriaNombre, opt => opt.MapFrom(src => src.Categoria != null ? src.Categoria.Nombre : string.Empty))
            .ForMember(dest => dest.NombreMostrar, opt => opt.MapFrom(src => src.Nombre ?? src.Descripcion));

        // Vendedores de temporada (nombre y username de AuthDB los llena el servicio)
        CreateMap<Vendedor, VendedorNavDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)))
            .ForMember(dest => dest.TipoNombre, opt => opt.MapFrom(src => src.Tipo.ToString()))
            .ForMember(dest => dest.UsuarioNombreCompleto, opt => opt.Ignore())
            .ForMember(dest => dest.Username, opt => opt.Ignore());
    }
}
