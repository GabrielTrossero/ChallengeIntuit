namespace TurnosMedicos.Helpers;

public static class DateTimeExtensions
{
    public static bool IsCancellable(this DateTime fechaTurno)
    {
        return (fechaTurno - DateTime.Now).TotalHours >= 24;
    }

    public static bool IsWithinNoShowWindow(this DateTime fechaTurno)
    {
        var hoursFromTurno = (DateTime.Now - fechaTurno).TotalHours;
        return hoursFromTurno >= 0 && hoursFromTurno <= 24;
    }
}
