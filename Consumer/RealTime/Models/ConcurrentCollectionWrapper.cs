using Consumer.RealTime.Models.Dtos;

namespace Consumer.RealTime.Models;

public sealed class ConcurrentCollectionWrapper<T>
    where T : Response
{
    private readonly IDictionary<string, T> _items = new Dictionary<string, T>();


    public T? TryGetAndRemove(string id)
    {
        lock (_items)
        {
            if (!_items.TryGetValue(id, out var value))
            {
                return null;
            }
            _items.Remove(id);

            return value;
        }
    }


    public void Add(T item)
    {
        lock (_items)
        {
            _items.Add(item.Id, item);
        }
    }

    public void Clear()
    {
        lock (_items)
        {
            _items.Clear();
        }
    }
}