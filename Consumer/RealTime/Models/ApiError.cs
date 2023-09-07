using System.Net;

namespace Consumer.RealTime.Models;

public sealed record ApiError(string Message, HttpStatusCode? StatusCode);