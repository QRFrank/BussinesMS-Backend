using AutoMapper;
using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Common;

namespace BussinesMS.Aplicacion.Mapeos;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Sistemas
        CreateMap<Sistema, SistemaDto>();
        CreateMap<SistemaDto, Sistema>();
        CreateMap<CrearSistemaDto, Sistema>();

        // Roles
        CreateMap<Rol, RolDto>();
        CreateMap<RolDto, Rol>();
        CreateMap<CrearRolDto, Rol>();

        // Almacenes
        CreateMap<Almacen, AlmacenDto>();
        CreateMap<AlmacenDto, Almacen>();
        CreateMap<CrearAlmacenDto, Almacen>();

        // Usuarios
        CreateMap<Usuario, UsuarioDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<UsuarioDto, Usuario>();
        CreateMap<CrearUsuarioDto, Usuario>();

        // Menus
        CreateMap<Menu, MenuDto>();
        CreateMap<MenuDto, Menu>();
        CreateMap<CrearMenuDto, Menu>();

        // Categorías
        CreateMap<Categoria, CategoriaDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<CategoriaDto, Categoria>();
        CreateMap<CrearCategoriaDto, Categoria>();
        CreateMap<ActualizarCategoriaDto, Categoria>();

        // Fabricantes
        CreateMap<Fabricante, FabricanteDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<FabricanteDto, Fabricante>();
        CreateMap<CrearFabricanteDto, Fabricante>();
        CreateMap<ActualizarFabricanteDto, Fabricante>();

        // Sabores
        CreateMap<DescripcionSabor, DescripcionSaborDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<DescripcionSaborDto, DescripcionSabor>();
        CreateMap<CrearDescripcionSaborDto, DescripcionSabor>();
        CreateMap<ActualizarDescripcionSaborDto, DescripcionSabor>();

        // Tamaños
        CreateMap<DescripcionTamanio, DescripcionTamanioDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<DescripcionTamanioDto, DescripcionTamanio>();
        CreateMap<CrearDescripcionTamanioDto, DescripcionTamanio>();
        CreateMap<ActualizarDescripcionTamanioDto, DescripcionTamanio>();

        // Productos
        CreateMap<Producto, ProductoDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<ProductoDto, Producto>();
        CreateMap<CrearProductoDto, Producto>();
        CreateMap<ActualizarProductoDto, Producto>();

        // Proveedores
        CreateMap<Proveedor, ProveedorDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<ProveedorDto, Proveedor>();
        CreateMap<CrearProveedorDto, Proveedor>();
        CreateMap<ActualizarProveedorDto, Proveedor>();

        // Clientes
        CreateMap<Cliente, ClienteDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<ClienteDto, Cliente>();
        CreateMap<CrearClienteDto, Cliente>();
        CreateMap<ActualizarClienteDto, Cliente>();

        // CategoriasGasto
        CreateMap<CategoriaGasto, CategoriaGastoDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<CrearCategoriaGastoDto, CategoriaGasto>();
        CreateMap<ActualizarCategoriaGastoDto, CategoriaGasto>();

        // ProductoVariantes
        CreateMap<ProductoVariante, ProductoVarianteDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<ProductoVarianteDto, ProductoVariante>();
        CreateMap<CrearProductoVarianteDto, ProductoVariante>();
        CreateMap<ActualizarProductoVarianteDto, ProductoVariante>();

        // Compras
        CreateMap<Compra, CompraDto>()
            .ForMember(dest => dest.ProveedorNombre, opt => opt.MapFrom(src => src.Proveedor != null ? src.Proveedor.Nombre : null))
            .ForMember(dest => dest.ProveedorNit, opt => opt.MapFrom(src => src.Proveedor != null ? src.Proveedor.Nit : null))
            .ForMember(dest => dest.ProveedorTelefono, opt => opt.MapFrom(src => src.Proveedor != null ? src.Proveedor.Telefono : null))
            .ForMember(dest => dest.UsuarioId, opt => opt.MapFrom(src => src.UsuarioId))
            .ForMember(dest => dest.FechaCompra, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.FechaCompra)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<CompraDto, Compra>();
        CreateMap<CrearCompraDto, Compra>()
            .ForMember(dest => dest.Detalles, opt => opt.Ignore());
        CreateMap<ActualizarCompraDto, Compra>();

        // CompraDetalles
        CreateMap<CompraDetalle, CompraDetalleDto>()
            .ForMember(dest => dest.FechaVencimiento, opt => opt.MapFrom(src => src.FechaVencimiento.HasValue ? BoliviaTimeZone.ToLocal(src.FechaVencimiento.Value) : (DateTime?)null))
            .AfterMap((src, dest) =>
            {
                dest.VarianteNombre = src.Variante != null
                    ? Helpers.DescripcionProductoBuilder.Construir(
                        src.Variante.Producto?.Nombre ?? "", src.Variante.Sabor?.Nombre ?? "",
                        src.Variante.Tamanio?.Nombre ?? "", src.Variante.CantidadCaja,
                        src.Variante.Producto?.Fabricante?.Nombre)
                    : null;
            });
        // PagosCompra
        CreateMap<PagoCompra, PagoCompraDto>()
            .ForMember(dest => dest.FechaPago, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.FechaPago)));
        CreateMap<CrearPagoCompraDto, PagoCompra>()
            .ForMember(dest => dest.Monto, opt => opt.MapFrom(src => src.MontoCaja + src.MontoExterno));

        // TipoPresentacion
        CreateMap<TipoPresentacion, TipoPresentacionDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<CrearTipoPresentacionDto, TipoPresentacion>();
        CreateMap<ActualizarTipoPresentacionDto, TipoPresentacion>();

        // ProductoPresentacion — el DTO se construye manualmente en el service
        // por eso solo necesitamos el mapeo inverso
        CreateMap<CrearProductoPresentacionDto, ProductoPresentacion>();
        CreateMap<ActualizarProductoPresentacionDto, ProductoPresentacion>();

        // MovimientoInventario
        CreateMap<MovimientoInventario, MovimientoInventarioDto>()
            .ForMember(dest => dest.FechaMovimiento, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.FechaMovimiento)))
            .ForMember(dest => dest.LoteId, opt => opt.MapFrom(src => src.LoteAlmacen != null ? src.LoteAlmacen.LoteId : 0))
            .AfterMap((src, dest) =>
            {
                dest.VarianteNombre = src.Variante != null
                    ? Helpers.DescripcionProductoBuilder.Construir(
                        src.Variante.Producto?.Nombre ?? "", src.Variante.Sabor?.Nombre ?? "",
                        src.Variante.Tamanio?.Nombre ?? "", src.Variante.CantidadCaja,
                        src.Variante.Producto?.Fabricante?.Nombre)
                    : null;
            });

        // DevolucionCliente
        CreateMap<DevolucionCliente, DevolucionClienteDto>()
            .ForMember(dest => dest.FechaDevolucion, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.FechaDevolucion)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));

        // SesionCaja
        CreateMap<SesionCaja, SesionCajaDto>()
            .ForMember(dest => dest.FechaApertura, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.FechaApertura)))
            .ForMember(dest => dest.FechaCierre, opt => opt.MapFrom(src => src.FechaCierre.HasValue ? BoliviaTimeZone.ToLocal(src.FechaCierre.Value) : (DateTime?)null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)));
        CreateMap<CrearSesionCajaDto, SesionCaja>();

        // Ventas
        CreateMap<Venta, VentaDto>()
            .ForMember(dest => dest.FechaVenta, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.FechaVenta)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => BoliviaTimeZone.ToLocal(src.CreatedAt)))
            .ForMember(dest => dest.ClienteNombre, opt => opt.MapFrom(src => src.Cliente != null ? src.Cliente.Nombre : null))
            .ForMember(dest => dest.ClienteCarnet, opt => opt.MapFrom(src => src.Cliente != null ? src.Cliente.NumeroCarnet : null))
            .ForMember(dest => dest.ClienteTelefono, opt => opt.MapFrom(src => src.Cliente != null ? src.Cliente.Telefono : null));
        CreateMap<CrearVentaDto, Venta>()
            .ForMember(dest => dest.Detalles, opt => opt.Ignore())
            .ForMember(dest => dest.ClienteId, opt => opt.Ignore()); // lo resuelve VentaService (null → cliente genérico)

        // VentaDetalles
        CreateMap<VentaDetalle, VentaDetalleDto>()
            .ForMember(dest => dest.CodigoBarras, opt => opt.MapFrom(src => src.Variante != null ? src.Variante.CodigoBarras : null))
            .AfterMap((src, dest) =>
            {
                dest.VarianteNombre = src.Variante != null
                    ? Helpers.DescripcionProductoBuilder.Construir(
                        src.Variante.Producto?.Nombre ?? "", src.Variante.Sabor?.Nombre ?? "",
                        src.Variante.Tamanio?.Nombre ?? "", src.Variante.CantidadCaja,
                        src.Variante.Producto?.Fabricante?.Nombre)
                    : null;
                dest.CodigoLote = src.Lote?.CodigoLote;
            });
    }
}
