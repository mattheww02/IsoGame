using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexLayersTest.Shared;

public static class Extensions
{
    public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> items)
    {
        foreach (var item in items) queue.Enqueue(item);
    }
}
