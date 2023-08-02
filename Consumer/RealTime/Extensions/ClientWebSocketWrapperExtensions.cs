using Consumer.RealTime.Models.Dtos;
using Consumer.RealTime.Services;

namespace Consumer.RealTime.Extensions;

public static class ClientWebSocketWrapperExtensions
{
    public static ValueTask Send(this IClientWebSocketWrapper clientWebSocket, Request request, CancellationToken cancellationToken = default) =>
        clientWebSocket.Send(request.AsUtf8Bytes(), cancellationToken);
}