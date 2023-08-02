namespace Consumer.RealTime.Models;

public sealed record Error(bool Transient, string Message);