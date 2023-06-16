using System;
using System.Collections.Generic;

namespace UnityEngine.UI.Collections
{
    public class IndexedSet<T> where T : class
    {
        //This is a container that gives:
        //  - Unique items
        //  - Fast random removal
        //  - Fast unique inclusion to the end
        //  - Sequential access
        //  - Possibility to have disabled items registered
        //Downsides:
        //  - Uses more memory
        //  - Ordering is not persistent
        //  - Not Serialization Friendly.

        //We use a Dictionary to speed up list lookup, this makes it cheaper to guarantee no duplicates (set)
        //When removing we move the last item to the removed item position, this way we only need to update the index cache of a single item. (fast removal)
        //Order of the elements is not guaranteed. A removal will change the order of the items.

        readonly List<T> m_List = new();
        readonly Dictionary<int, int> m_IndexMap = new();

        public void Add(T item)
        {
            var orgItemCount = m_List.Count;
            m_List.Add(item);
            m_IndexMap.Add(item.GetHashCode(), orgItemCount);
        }

        public bool TryAdd(T item)
        {
            var hashCode = item.GetHashCode();
            var orgItemCount = m_List.Count;
            if (m_IndexMap.TryAdd(hashCode, orgItemCount) == false)
                return false;
            m_List.Add(item);
            return true;
        }

        public void Remove(T item)
        {
            var removed = TryRemove(item);
            if (removed == false)
                throw new ArgumentException("Item doesn't exist in the IndexedSet");
        }

        public bool TryRemove(T item)
        {
            var hashCode = item.GetHashCode();

            // If the item is not in the list, throw an exception.
            if (m_IndexMap.Remove(hashCode, out var index) == false)
                return false;

            // If the item is the last item, just remove it.
            var orgCount = m_List.Count;
            if (orgCount == 1)
            {
                m_List.Clear();
                return true;
            }

            // Move the last item to the removed item position.
            if (index != orgCount - 1)
            {
                var lastItem = m_List[orgCount - 1];
                m_List[index] = lastItem;
                m_IndexMap[lastItem.GetHashCode()] = index;
            }

            // Remove the last item.
            m_List.RemoveAt(orgCount - 1);
            return true;
        }

        public void Clear()
        {
            m_List.Clear();
            m_IndexMap.Clear();
        }

        public bool Contains(T item)
        {
            return m_IndexMap.ContainsKey(item.GetHashCode());
        }

        public int Count => m_List.Count;

        public List<T>.Enumerator GetEnumerator() => m_List.GetEnumerator();

        public void Flush(List<T> list)
        {
            list.AddRange(m_List);
            Clear();
        }
    }
}