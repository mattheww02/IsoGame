using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HexLayersTest.Shared;

public class BlockingHashSet<T> : IEnumerable<T>
{
    private readonly HashSet<T> _set = new();
    private readonly object _lock = new();

    public bool RemoveAndAdd(T itemToRemove, T itemToAdd)
    {
        lock (_lock)
        {
            if (!_set.Contains(itemToRemove) || _set.Contains(itemToAdd)) return false;
            _set.Remove(itemToRemove);
            _set.Add(itemToAdd);
            Monitor.PulseAll(_lock);
            return true;
        }
    }

    public bool Add(T item)
    {
        lock (_lock)
        {
            bool added = _set.Add(item);
            if (added) Monitor.PulseAll(_lock); // wake up any blocked threads
            return added;
        }
    }

    public bool Remove(T item)
    {
        lock (_lock)
        {
            return _set.Remove(item);
        }
    }

    public T TakeAny()
    {
        lock (_lock)
        {
            while (_set.Count == 0)
                Monitor.Wait(_lock);

            var item = _set.First();
            _set.Remove(item);
            return item;
        }
    }

    public bool TryTakeAny(out T item, int millisecondsTimeout = Timeout.Infinite)
    {
        lock (_lock)
        {
            if (_set.Count == 0)
            {
                if (!Monitor.Wait(_lock, millisecondsTimeout))
                {
                    item = default!;
                    return false;
                }
            }

            if (_set.Count == 0)
            {
                item = default!;
                return false;
            }

            item = _set.First();
            _set.Remove(item);
            return true;
        }
    }

    public bool Contains(T item)
    {
        lock (_lock)
        {
            return _set.Contains(item);
        }
    }

    public int Count
    {
        get
        {
            lock (_lock) return _set.Count;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock)
        {
            return _set.ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Clear()
    {
        lock (_lock)
        {
            _set.Clear();
        }
    }
}

