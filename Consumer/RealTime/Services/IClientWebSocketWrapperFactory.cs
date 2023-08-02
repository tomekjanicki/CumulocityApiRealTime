using System.Net;

namespace Consumer.RealTime.Services;

public interface IClientWebSocketWrapperFactory
{
    IClientWebSocketWrapper GetNewInstance(ICredentials credentials);
}