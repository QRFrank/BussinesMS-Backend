namespace BussinesMS.API.Autorizacion;

// Flag de UsuarioMenus que exige un endpoint. PorVerbo: GET=Leer, POST=Crear, PUT/PATCH=Editar, DELETE=Eliminar.
public enum FlagMenu
{
    PorVerbo = 0,
    Leer = 1,
    Crear = 2,
    Editar = 3,
    Eliminar = 4
}
