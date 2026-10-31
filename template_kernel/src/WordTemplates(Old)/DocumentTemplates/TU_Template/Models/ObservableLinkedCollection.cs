using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace TU_Template.Models;

public class ObservableLinkedCollection<TContainer, TElement> :
    IList<ObservableLinkedCollection<TContainer, TElement>.CollectionElement>,
    IReadOnlyList<ObservableLinkedCollection<TContainer, TElement>.CollectionElement>,
    IList<TElement>,
    IReadOnlyList<TElement>,
    IList,
    INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly ObservableCollection<CollectionElement> _collection;

    public ObservableLinkedCollection(TContainer container, IEnumerable<TElement> collection)
    {
        Container = container;
        _collection = new(collection.Select(e => new CollectionElement(e, Container)));
    }

    public ObservableLinkedCollection(TContainer container) : this(container, [ ]) { }

    public TContainer Container { get; }

    public record struct CollectionElement(TElement Value, TContainer Container);

    public bool IsReadOnly => false;

    public bool IsFixedSize => false;

    public bool IsSynchronized => ((IList)_collection).IsSynchronized;
    public object SyncRoot => ((IList)_collection).SyncRoot;

    public int Count => _collection.Count;
    int ICollection<CollectionElement>.Count => _collection.Count;
    int IReadOnlyCollection<CollectionElement>.Count => _collection.Count;

    IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() => _collection.Select(e => e.Value).GetEnumerator();

    public IEnumerator<CollectionElement> GetEnumerator() => _collection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Clear() => _collection.Clear();

    public void Add(CollectionElement item) => _collection.Add(new(item.Value, Container));
    public void Add(TElement item) => _collection.Add(new(item, Container));
    public int Add(object? value) => ((IList)_collection).Add(value);

    public bool Contains(TElement item) => _collection.Contains(new(item, Container));
    public bool Contains(CollectionElement item) => _collection.Contains(new(item.Value, Container));
    public bool Contains(object? value) => ((IList)_collection).Contains(value);

    public void CopyTo(TElement[] array, int arrayIndex) =>
        Array.Copy(_collection.Select(e => e.Value).ToArray(), 0, array, arrayIndex, _collection.Count);

    public void CopyTo(CollectionElement[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

    public void CopyTo(Array array, int index) => ((ICollection)_collection).CopyTo(array, index);

    public bool Remove(CollectionElement item) => _collection.Remove(new(item.Value, Container));
    public bool Remove(TElement item) => _collection.Remove(new(item, Container));
    public void Remove(object? value) => ((IList)_collection).Remove(value);

    public void RemoveAt(int index) => _collection.RemoveAt(index);

    public int IndexOf(CollectionElement item) => _collection.IndexOf(new(item.Value, Container));
    public int IndexOf(TElement item) => _collection.IndexOf(new(item, Container));
    public int IndexOf(object? value) => ((IList)_collection).IndexOf(value);

    public void Insert(int index, CollectionElement item) => _collection.Insert(index, new(item.Value, Container));
    public void Insert(int index, TElement item) => _collection.Insert(index, new(item, Container));
    public void Insert(int index, object? value) => ((IList)_collection).Insert(index, value);

    TElement IList<TElement>.this[int index]
    {
        get => _collection[index].Value;
        set => _collection[index] = new(value, Container);
    }

    public CollectionElement this[int index]
    {
        get => _collection[index];
        set => _collection[index] = new(value.Value, Container);
    }

    object? IList.this[int index]
    {
        get => _collection[index];
        set => _collection[index] = new CollectionElement((TElement)value!, Container);
    }

    TElement IReadOnlyList<TElement>.this[int index] => _collection[index].Value;

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => _collection.CollectionChanged += value;
        remove => _collection.CollectionChanged -= value;
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => ((INotifyPropertyChanged)_collection).PropertyChanged += value;
        remove => ((INotifyPropertyChanged)_collection).PropertyChanged -= value;
    }
}