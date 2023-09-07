using Consumer.RealTime.Services;

namespace Consumer.RealTime.Models.Dtos;

public sealed class Auth
{
    public string Token { get; init; } = string.Empty;

    public static Auth GetWithUserNameAndPassword(string userName, string password) =>
        new() { Token = Helper.GetEncodedUserNameAndPassword(userName, password) };
}