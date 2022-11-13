namespace BankParser.ViewModels;

public class NotifyingList<TValue> : INotifyCollectionChanged, IList<TValue>, IEnumerable
{
    private readonly List<TValue> _items = new();

    public NotifyingList()
    {
    }

    public NotifyingList(IEnumerable<TValue> enumerable)
    {
        _items.AddRange(enumerable);
    }

    private void OnCollectionChanged(
        NotifyCollectionChangedAction action,
        IEnumerable<TValue> newValues,
        IEnumerable<TValue> oldValues,
        int index=-1)
    { switch (action) {
            case NotifyCollectionChangedAction.Reset:
                _collectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(action));
                break;
            case NotifyCollectionChangedAction.Replace:
                _collectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        action,
                        newValues,
                        oldValues));
                break;
            case NotifyCollectionChangedAction.Add when newValues.Count() > 1:

                _collectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        action,
                        newValues.ToList(),
                        index));
                break;
            case NotifyCollectionChangedAction.Add when newValues.Count() > 0:

                _collectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        action,
                        newValues.First(),
                        index));
                break;
            case NotifyCollectionChangedAction.Remove:
                _collectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        action,
                        oldValues.First(),
                        index));
                break;
        }
    }



    public TValue this[int index]
    {
        get => _items[index];
        set
        {
            if (value is null)
            {
                return;
            }

            TValue oldValue = _items[index];

            _items[index] = value;

            OnCollectionChanged(
                NotifyCollectionChangedAction.Replace,
                new[] { value,},
                new[] { oldValue,});
        }
    }

    public int Count => _items.Count;
    public bool IsReadOnly => false;

    private event NotifyCollectionChangedEventHandler? _collectionChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => _collectionChanged += value;
        remove => _collectionChanged -= value;
    }

    public void Add(TValue item)
    {
        _items.Add(item);

        OnCollectionChanged(
            NotifyCollectionChangedAction.Add,
            new[] { item,},
            Array.Empty<TValue>(),
            _items.Count-1);

    }

    public void AddRange(IEnumerable<TValue> items)
    {
        int index = _items.Count;
        _items.AddRange(items);

        OnCollectionChanged(
            NotifyCollectionChangedAction.Add,
            items.ToArray(),
            Array.Empty<TValue>(),
            index);
    }

    public void Clear()
    {
        TValue[] oldValues = _items.ToArray();
        _items.Clear();

        OnCollectionChanged(
            NotifyCollectionChangedAction.Reset,
            Array.Empty<TValue>(),
            oldValues);

    }
    public bool Contains(TValue item) => _items.Contains(item);
    public void CopyTo(TValue[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
    public IEnumerator<TValue> GetEnumerator() => _items.GetEnumerator();
    public int IndexOf(TValue item) => _items.IndexOf(item);
    public void Insert(int index, TValue item)
    {
        _items.Insert(index, item);

        OnCollectionChanged(
            NotifyCollectionChangedAction.Add,
            new[] { item,},
            Array.Empty<TValue>(),
            index);
    }

    public bool Remove(TValue item)
    {
        int index = _items.IndexOf(item);
        if (_items.Remove(item))
        {
            OnCollectionChanged(
                NotifyCollectionChangedAction.Remove,
                Array.Empty<TValue>(),
                new[] { item,},
                index
                );

            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        TValue item = _items[index];
        _items.RemoveAt(index);

        OnCollectionChanged(
            NotifyCollectionChangedAction.Remove,
            Array.Empty<TValue>(),
            new[] { item,},
            index
            );


    }
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
