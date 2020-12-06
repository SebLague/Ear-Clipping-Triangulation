using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// Credit to Brent
// https://stackoverflow.com/a/25048154/326698
public class OrderedList<T> : IList<T>, ICollection<T>, IList, ICollection, IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
{
    #region Fields

    private readonly List<T> _list;
    private readonly IComparer<T> _comparer;

    #endregion Fields

    #region Constructors

    private OrderedList(List<T> list, IComparer<T> comparer)
    {
        _list = list;
        _comparer = comparer;
    }

    public OrderedList()
        : this(new List<T>(), Comparer<T>.Default)
    {
    }

    public OrderedList(IComparer<T> comparer)
        : this(new List<T>(), comparer)
    {
    }

    public OrderedList(IEnumerable<T> collection)
        : this(collection, Comparer<T>.Default)
    {
    }

    public OrderedList(IEnumerable<T> collection, IComparer<T> comparer)
        : this(new List<T>(collection), comparer)
    {
        _list.Sort(comparer);
    }

    public OrderedList(int capacity)
        : this(new List<T>(capacity), Comparer<T>.Default)
    {
    }

    public OrderedList(int capacity, IComparer<T> comparer)
        : this(new List<T>(capacity), comparer)
    {
    }

    //yet to be implemented
    //public void OrderedList(Comparison<T> comparison);

    #endregion Constructors

    #region Properties

    public int Capacity { get { return _list.Capacity; } set { _list.Capacity = value; } }
    public int Count { get { return _list.Count; } }
    object IList.this[int index] { get { return _list[index]; } set { _list[index] = (T)value; } }
    public T this[int index] { get { return _list[index]; } set { _list[index] = value; } }

    //public bool IsSynchronized { get { return false; } }
    bool ICollection.IsSynchronized { get { return false; } }

    //public object SyncRoot { get { return _list; } }
    object ICollection.SyncRoot { get { return _list; } } //? should return this

    bool IList.IsFixedSize { get { return false; } }
    bool IList.IsReadOnly { get { return false; } }
    bool ICollection<T>.IsReadOnly { get { return false; } }

    #endregion Properties

    #region Methods

    void ICollection<T>.Add(T item)
    {
        Add(item);
    }

    /// <summary>
    /// Adds a new item to the appropriate index of the SortedList
    /// </summary>
    /// <param name="item">The item to be removed</param>
    /// <returns>The index at which the item was inserted</returns>
    public int Add(T item)
    {
        int index = BinarySearch(item);
        if (index < 0)
        {
            index = ~index;
        }
        _list.Insert(index, item);
        return index;
    }

    int IList.Add(object item)
    {
        return Add((T)item);
    }

    //NOT performance tested against other ways algorithms yet
    public void AddRange(IEnumerable<T> collection)
    {
        var insertList = new List<T>(collection);
        if (insertList.Count == 0)
        {
            return;
        }
        if (_list.Count == 0)
        {
            _list.AddRange(collection);
            _list.Sort(_comparer);
            return;
        }
        //if we insert backwards, index we are inserting at does not keep incrementing
        insertList.Sort(_comparer);
        int searchLength = _list.Count;
        for (int i = insertList.Count - 1; i >= 0; i--)
        {
            T item = insertList[i];
            int insertIndex = BinarySearch(0, searchLength, item);
            if (insertIndex < 0)
            {
                insertIndex = ~insertIndex;
            }
            else
            {
                while (--insertIndex >= 0 && _list[insertIndex].Equals(item)) { }
                insertIndex++;
            }
            if (insertIndex <= 0)
            {
                _list.InsertRange(0, insertList.GetRange(0, i + 1));
                break;
            }
            searchLength = insertIndex - 1;
            item = _list[searchLength];
            int endInsert = i;
            while (--i >= 0 && _comparer.Compare(insertList[i], item) > 0) { }
            i++;
            _list.InsertRange(insertIndex, insertList.GetRange(i, endInsert - i + 1));
        }
    }

    public int BinarySearch(T item)
    {
        return _list.BinarySearch(item, _comparer);
    }

    public int BinarySearch(int index, int count, T item)
    {
        return _list.BinarySearch(index, count, item, _comparer);
    }

    public ReadOnlyCollection<T> AsReadOnly()
    {
        return _list.AsReadOnly();
    }

    public void Clear()
    {
        _list.Clear();
    }

    public bool Contains(T item)
    {
        return BinarySearch(item) >= 0;
    }

    bool IList.Contains(object item)
    {
        return Contains((T)item);
    }

    public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
    {
        return _list.ConvertAll(converter);
    }

    public void CopyTo(T[] array)
    {
        _list.CopyTo(array);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    void ICollection.CopyTo(Array array, int arrayIndex)
    {
        _list.CopyTo((T[])array, arrayIndex);
    }

    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
        _list.CopyTo(index, array, arrayIndex, count);
    }

    public void ForEach(Action<T> action)
    {
        foreach (T item in _list)
        {
            action(item);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    public List<T> GetRange(int index, int count)
    {
        return _list.GetRange(index, count);
    }

    public bool Remove(T item)
    {
        int index = BinarySearch(item);
        if (index < 0)
        {
            return false;
        }
        _list.RemoveAt(index);
        return true;
    }

    void IList.Remove(object item)
    {
        Remove((T)item);
    }

    public void RemoveAt(int index)
    {
        _list.RemoveAt(index);
    }

    public void RemoveRange(int index, int count)
    {
        _list.RemoveRange(index, count);
    }

    public T[] ToArray()
    {
        return _list.ToArray();
    }

    public void TrimExcess()
    {
        _list.TrimExcess();
    }

    /// <summary>
    /// Find the first index of the given item
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public int IndexOf(T item)
    {
        int index = BinarySearch(item);
        if (index < 0) return -1;
        while (--index >= 0 && _list[index].Equals(item)) { }
        return index + 1;
    }

    int IList.IndexOf(object item)
    {
        return IndexOf((T)item);
    }

    /// <summary>
    /// Find the last index of the given item
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public int LastIndexOf(T item)
    {
        int index = BinarySearch(item);
        if (index < 0) return -1;
        while (++index < _list.Count && _list[index].Equals(item)) { }
        return index - 1;
    }

    /// <summary>
    /// Return all values within bounds specified
    /// </summary>
    /// <param name="min">Minimum Bound</param>
    /// <param name="max">Maximum Bound</param>
    /// <returns>subset of list with values within or equal to bounds specified</returns>
    public T[] WithinRange(T min, T max)
    {
        if (_comparer.Compare(min, max) > 0)
        {
            throw new ArgumentException("min must be <= max");
        }
        int minSearchLength;
        int maxIndex = _list.BinarySearch(max, _comparer);
        if (maxIndex >= 0)
        {
            minSearchLength = maxIndex + 1;
            while (++maxIndex < _list.Count && _comparer.Compare(max, _list[maxIndex]) == 0) { }
            --maxIndex;
        }
        else
        {
            minSearchLength = ~maxIndex;
            if (minSearchLength <= 0)
            {
                return new T[0];
            }
            maxIndex = minSearchLength - 1;
        }

        int minIndex = _list.BinarySearch(0, minSearchLength, min, _comparer);
        if (minIndex >= 0)
        {
            while (--minIndex >= 0 && _comparer.Compare(max, _list[minIndex]) == 0) { }
            ++minIndex;
        }
        else
        {
            minIndex = ~minIndex;
            if (minIndex > maxIndex)
            {
                return new T[0];
            }
        }
        int length = maxIndex - minIndex + 1;
        var returnVar = new T[length];
        _list.CopyTo(minIndex, returnVar, 0, length);
        return returnVar;
    }

    #endregion Methods

    #region NotImplemented

    private const string _insertExceptionMsg = "SortedList detemines position to insert automatically - use add method without an index";

    void IList.Insert(int index, object item)
    {
        throw new NotImplementedException(_insertExceptionMsg);
    }

    void IList<T>.Insert(int index, T item)
    {
        throw new NotImplementedException(_insertExceptionMsg);
    }

    #endregion NotImplemented
}