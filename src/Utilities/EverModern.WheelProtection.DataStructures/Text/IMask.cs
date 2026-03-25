namespace EverModern.WheelProtection.DataStructures.Text;

/// <summary>
/// Represents a mutable mask with editable slots.
/// </summary>
/// <typeparam name="TSymbol">The symbol type.</typeparam>
public interface IMask<TSymbol>
{
    /// <summary>
    /// Gets the mask slots.
    /// </summary>
    IReadOnlyList<TSymbol> Slots { get; }

    /// <summary>
    /// Applies a content change and returns the caret position.
    /// </summary>
    /// <param name="contentChange">The change to apply.</param>
    int AcceptChange(ContentChange<TSymbol> contentChange);
}

/// <summary>
/// Represents an immutable mask that can produce modified copies.
/// </summary>
/// <typeparam name="TSymbol">The symbol type.</typeparam>
/// <typeparam name="TMask">The mask type.</typeparam>
public interface IImmutableMask<TSymbol, TMask> : IReadOnlyList<TSymbol>
    where TMask : IImmutableMask<TSymbol, TMask>
{
    /// <summary>
    /// Applies a content change and returns the new mask.
    /// </summary>
    /// <param name="contentChange">The change to apply.</param>
    /// <param name="caretPosition">The caret position after the change.</param>
    TMask Change(ContentChange<TSymbol> contentChange, out int caretPosition);
}
