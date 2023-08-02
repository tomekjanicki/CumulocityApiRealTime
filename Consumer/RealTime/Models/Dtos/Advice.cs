namespace Consumer.RealTime.Models.Dtos;

public sealed class Advice
{
    public int Timeout { get; init; }

    public int Interval { get; init; }

    public bool IsTimeOuted(HeartBeatTimes heartBeatTimes, DateTime now, TimeSpan monitorInterval)
    {
        var startDateTime = heartBeatTimes.Start;
        var endDateTime = heartBeatTimes.End;

        return startDateTime is not null && IsTimeOuted(startDateTime.Value, endDateTime, now, monitorInterval);
    }

    private bool IsTimeOuted(DateTime startDateTime, DateTime? endDateTime, DateTime now, TimeSpan monitorInterval)
    {
        if (endDateTime is not null)
        {
            return endDateTime.Value < startDateTime ? IsTimeOuted(startDateTime, now, monitorInterval) : IsTimeOuted(startDateTime, endDateTime.Value, TimeSpan.Zero);
        }

        return IsTimeOuted(startDateTime, now, monitorInterval);
    }

    private bool IsTimeOuted(DateTime start, DateTime end, TimeSpan monitorInterval)
        => end.Subtract(start) > monitorInterval + TimeSpan.FromMilliseconds(Timeout + Interval);
}