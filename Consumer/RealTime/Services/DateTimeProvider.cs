namespace Consumer.RealTime.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime GetNow() => 
        DateTime.Now;
}