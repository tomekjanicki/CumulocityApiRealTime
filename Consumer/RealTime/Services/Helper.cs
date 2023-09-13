using System.Text;

namespace Consumer.RealTime.Services;

public static class Helper
{
    public static string GetEncodedUserNameAndPassword(string userName, string password) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));

    public static CancellationTokenSource CreateCancellationTokenSource(CancellationToken token, TimeSpan timeout) =>
        CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(timeout).Token);
}