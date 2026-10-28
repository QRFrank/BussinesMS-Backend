namespace BussinesMS.Aplicacion.Seguridad;

// Marca un campo de una respuesta navideña que expone precio de compra, costo o un monto derivado del costo.
// Si el usuario no tiene el permiso "Ver precios de compra" el filtro de la API lo deja en null.
// La propiedad tiene que ser nullable.
[AttributeUsage(AttributeTargets.Property)]
public class OcultoSinPrecioCompraAttribute : Attribute
{
}
