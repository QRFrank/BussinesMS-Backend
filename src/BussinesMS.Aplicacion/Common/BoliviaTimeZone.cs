namespace BussinesMS.Aplicacion.Common;

public static class BoliviaTimeZone
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(-4);

    public static DateTime Now() => ToLocal(DateTime.UtcNow);

    public static DateTime ToLocal(DateTime utc)
        => DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(Offset);

    public static DateTime ToUtc(DateTime local)
        => DateTime.SpecifyKind(local - Offset, DateTimeKind.Utc);

    public static (DateTime InicioUtc, DateTime FinUtc) RangoDiaUtc(DateOnly diaLocal)
    {
        var inicioLocal = diaLocal.ToDateTime(TimeOnly.MinValue);
        return (ToUtc(inicioLocal), ToUtc(inicioLocal.AddDays(1)));
    }

    public static (DateTime InicioUtc, DateTime FinUtc) RangoFechasUtc(DateOnly desde, DateOnly hasta)
        => (ToUtc(desde.ToDateTime(TimeOnly.MinValue)), ToUtc(hasta.ToDateTime(TimeOnly.MinValue).AddDays(1)));
}
