namespace DestallMaterials.WheelProtection.DataStructures.Text;

public interface IMask<TSymbol>
{
    IReadOnlyList<TSymbol> Slots { get; }

    int AcceptChange(ContentChange<TSymbol> contentChange);
}