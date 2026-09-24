using AutoMapper;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using BussinesMS.Dominio.Excepciones;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class SesionCajaService : ISesionCajaService
{
    private readonly ISesionCajaRepository _repo;
    private readonly IMapper _mapper;
    private readonly ILogger<SesionCajaService> _logger;
    private readonly ICurrentUserService _currentUser;

    public SesionCajaService(
        ISesionCajaRepository repo,
        IMapper mapper,
        ILogger<SesionCajaService> logger,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<SesionCajaDto> AbrirCajaAsync(CrearSesionCajaDto dto)
    {
        try
        {
            var usuarioId = _currentUser.GetUsuarioId() ?? 1;

            var existente = await _repo.ObtenerAbiertaPorUsuarioAsync(usuarioId, dto.AlmacenId);
            if (existente != null)
                throw new ValidacionException("Ya existe una sesión de caja abierta para este usuario en este almacén");

            var entidad = new SesionCaja
            {
                AlmacenId = dto.AlmacenId,
                MontoInicial = dto.MontoInicial,
                Estado = EstadoSesionCaja.Abierta
            };

            var resultado = await _repo.CrearAsync(entidad);
            return _mapper.Map<SesionCajaDto>(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al abrir sesión de caja");
            throw;
        }
    }

    public async Task<SesionCajaDto> CerrarCajaAsync(int id, CerrarSesionCajaDto dto)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null)
                throw new EntidadNoEncontradaException("SesionCaja", id);

            ValidacionEntidad.VerificarActivo(entidad, "Sesión de caja");

            if (entidad.Estado != EstadoSesionCaja.Abierta)
                throw new ValidacionException("Solo se pueden cerrar sesiones abiertas");

            var montoEsperado = entidad.MontoInicial
                + entidad.IngresosEfectivo
                - entidad.EgresosGastos
                - entidad.EgresosPagoProveedor;

            entidad.FechaCierre = DateTime.UtcNow;
            entidad.MontoEsperadoEfectivo = montoEsperado;
            entidad.MontoRealEntregado = dto.MontoRealEntregado;
            entidad.Diferencia = dto.MontoRealEntregado - montoEsperado;
            entidad.Estado = EstadoSesionCaja.Cerrada;

            var resultado = await _repo.ActualizarAsync(entidad);
            return _mapper.Map<SesionCajaDto>(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cerrar sesión de caja {Id}", id);
            throw;
        }
    }

    public async Task<SesionCajaDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            return entidad == null ? null : _mapper.Map<SesionCajaDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sesión de caja {Id}", id);
            throw;
        }
    }

    public async Task<SesionCajaDto?> ObtenerAbiertaAsync(int usuarioId, int almacenId)
    {
        try
        {
            var entidad = await _repo.ObtenerAbiertaPorUsuarioAsync(usuarioId, almacenId);
            return entidad == null ? null : _mapper.Map<SesionCajaDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sesión abierta del usuario {UsuarioId}", usuarioId);
            throw;
        }
    }
}
