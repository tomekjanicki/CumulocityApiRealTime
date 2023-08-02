using System.Net;

namespace Consumer.RealTime.Services;

public sealed class ClientWebSocketWrapperFactory : IClientWebSocketWrapperFactory
{
    public IClientWebSocketWrapper GetNewInstance(ICredentials credentials) => 
        new ClientWebSocketWrapper(credentials);
}