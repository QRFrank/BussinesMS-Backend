using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Auth;

namespace BussinesMS.Aplicacion.Interfaces.Auth;

public interface IWhatsAppInstanciaRepository : IRepositorio<WhatsAppInstancia>
{
    /// <summary>La instancia vigente (IsActive = 1) o null.</summary>
    Task<WhatsAppInstancia?> ObtenerVigenteAsync();
}
