using Consumer.RealTime.Services;

namespace Consumer.RealTime.Models;

public sealed class HeartBeatTimes
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public HeartBeatTimes(IDateTimeProvider dateTimeProvider) =>
        _dateTimeProvider = dateTimeProvider;

    public DateTime? Start { get; private set; }

    public DateTime? End { get; private set; }

    public void SetStart() => Start = _dateTimeProvider.GetNow();

    public void SetEnd() => End = _dateTimeProvider.GetNow();

    public void Clear()
    {
        Start = null;
        End = null;
    }

    public override string ToString() => 
        $"Start: {GetText(Start)}, End: {GetText(End)}";

    private static string GetText(DateTime? value) => 
        value is null ? "null" : value.Value.ToString("O");
}