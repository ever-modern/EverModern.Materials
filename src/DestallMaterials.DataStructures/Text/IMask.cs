namespace DestallMaterials.WheelProtection.DataStructures.Text;

public interface IMask<TSymbol>
{
    IReadOnlyList<TSymbol> Slots { get; }

    int AcceptChange(ContentChange<TSymbol> contentChange);
}

public interface IImmutableMask<TSymbol, TMask> : IReadOnlyList<TSymbol>
    where TMask : IImmutableMask<TSymbol, TMask>
{
    TMask Change(ContentChange<TSymbol> contentChange, out int caretPosition);
}
