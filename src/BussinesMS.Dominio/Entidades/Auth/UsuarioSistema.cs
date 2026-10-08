namespace BussinesMS.Dominio.Entidades.Auth;

public class UsuarioSistema
{
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int SistemaId { get; set; }
    public Sistema? Sistema { get; set; }
}
