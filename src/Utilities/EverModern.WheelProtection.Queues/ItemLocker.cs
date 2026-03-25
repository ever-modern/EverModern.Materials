using System;

namespace EverModern.WheelProtection.Queues;

/// <summary>
/// Represents a disposable lock handle for an item.
/// </summary>
public abstract class ItemLocker : IDisposable
{
    /// <inheritdoc />
    public abstract void Dispose();
}

/// <summary>
/// Tool that releases item for usage elsewhere on disposition.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public abstract class ItemLocker<T> : ItemLocker
{
    /// <summary>
    /// Gets the locked item.
    /// </summary>
    public T Item { get; }

    /// <summary>
    /// Initializes a new instance of the item locker.
    /// </summary>
    /// <param name="item">The locked item.</param>
    protected ItemLocker(T item)
    {
        Item = item;
    }

    /// <summary>
    /// Implicitly converts the locker to the wrapped item.
    /// </summary>
    /// <param name="itemLocker">The item locker.</param>
    public static implicit operator T(ItemLocker<T> itemLocker) => itemLocker.Item;

    /// <inheritdoc />
    public override sealed string ToString() => Item.ToString();
    /// <inheritdoc />
    public override sealed bool Equals(object obj) => Item.Equals(obj);
    /// <inheritdoc />
    public override sealed int GetHashCode() => Item.GetHashCode();
}

/// <summary>
/// Item locker that invokes a callback when disposed.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class CallbackItemLocker<T> : ItemLocker<T>
{
    readonly Action<CallbackItemLocker<T>> _onDisposed;
    /// <summary>
    /// Initializes a new instance of the callback item locker.
    /// </summary>
    /// <param name="item">The locked item.</param>
    /// <param name="onDisposed">The dispose callback.</param>
    public CallbackItemLocker(T item, Action<CallbackItemLocker<T>> onDisposed)
        : base(item)
    {
        _onDisposed = onDisposed;
    }

    /// <inheritdoc />
    public override sealed void Dispose() => _onDisposed(this);
}
