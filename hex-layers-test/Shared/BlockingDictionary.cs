using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HexLayersTest.Shared;

public class BlockingDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    private readonly Dictionary<TKey, TValue> _dictionary = [];
    private readonly object _lock = new();

    public bool RemoveAndAdd(TKey itemToRemove, TKey itemToAdd)
    {
        lock (_lock)
        {
            if (!_dictionary.ContainsKey(itemToRemove) || _dictionary.ContainsKey(itemToAdd)) return false;
            _dictionary.Remove(itemToRemove, out var value);
            _dictionary.Add(itemToAdd, value);
            Monitor.PulseAll(_lock);
            return true;
        }
    }

    public bool Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            bool added = _dictionary.TryAdd(key, value);
            if (added) Monitor.PulseAll(_lock); // wake up any blocked threads
            return added;
        }
    }

    public bool Remove(TKey item)
    {
        lock (_lock)
        {
            return _dictionary.Remove(item);
        }
    }

    public bool Contains(TKey item)
    {
        lock (_lock)
        {
            return _dictionary.ContainsKey(item);
        }
    }

    public bool TryGetValue(TKey item, out TValue value)
    {
        lock (_lock)
        {
            return _dictionary.TryGetValue(item, out value);
        }
    }

    public int Count
    {
        get
        {
            lock (_lock) return _dictionary.Count;
        }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        lock (_lock)
        {
            return _dictionary.ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Clear()
    {
        lock (_lock)
        {
            _dictionary.Clear();
        }
    }
}

