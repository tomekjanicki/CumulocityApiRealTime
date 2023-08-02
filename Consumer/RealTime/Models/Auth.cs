using System.Text;

namespace Consumer.RealTime.Models;

public sealed class Auth
{
    public string Token { get; init; } = string.Empty;

    public static Auth GetWithUserNameAndPassword(string userName, string password) => 
        new() { Token = GetEncodedUserNameAndPassword(userName, password) };

    private static string GetEncodedUserNameAndPassword(string userName, string password) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));
}