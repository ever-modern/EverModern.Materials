using System;

namespace DestallMaterials.WheelProtection.Queues;

public abstract class ItemLocker : IDisposable
{
    public abstract void Dispose();
}

/// <summary>
/// Tool that releases item for usage elsewhere on disposition.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ItemLocker<T> : ItemLocker
{
    public T Item { get; }

    protected ItemLocker(T item)
    {
        Item = item;
    }

    public static implicit operator T(ItemLocker<T> itemLocker) => itemLocker.Item;

    public override sealed string ToString() => Item.ToString();
    public override sealed bool Equals(object obj) => Item.Equals(obj);
    public override sealed int GetHashCode() => Item.GetHashCode();
}

public class CallbackItemLocker<T> : ItemLocker<T>
{
    readonly Action<CallbackItemLocker<T>> _onDisposed;
    public CallbackItemLocker(T item, Action<CallbackItemLocker<T>> onDisposed)
        : base(item)
    {
        _onDisposed = onDisposed;
    }

    public override sealed void Dispose() => _onDisposed(this);
}
