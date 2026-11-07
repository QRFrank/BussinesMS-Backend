using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IIncobrableNavService
{
    // temporadaId null = la abierta; tipo 1 = Cliente, 2 = VendedorRuta
    Task<List<IncobrableNavDto>> ObtenerAsync(int? temporadaId, int? tipo, bool incluirAnulados);
    Task<IncobrableNavDto> RegistrarClienteAsync(RegistrarIncobrableClienteNavDto dto);
    Task<IncobrableNavDto> RegistrarVendedorRutaAsync(RegistrarIncobrableVendedorRutaNavDto dto);
    Task<IncobrableNavDto> AnularAsync(int id, string? motivo);
}
