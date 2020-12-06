using System.Collections.Generic;

namespace Sebastian.Geometry
{
    public static class LinkedListExtensions
    {
        public static IEnumerable<LinkedListNode<T>> GetNodes<T>(this LinkedList<T> list)
        {
            for (var node = list.First; node != null; node = node.Next)
                yield return node;
        }
    }
}