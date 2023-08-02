namespace Consumer.RealTime.Models;

public sealed class HeartBeatTimes
{
    public DateTime? Start { get; private set; }

    public DateTime? End { get; private set; }

    public void SetStart() => Start = DateTime.Now;

    public void SetEnd() => End = DateTime.Now;

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