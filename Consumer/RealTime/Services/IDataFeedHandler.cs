namespace Consumer.RealTime.Services;

public interface IDataFeedHandler
{
    Task Handle(byte[] data, CancellationToken cancellationToken);
}