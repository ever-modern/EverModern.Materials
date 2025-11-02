namespace DestallMaterials.WheelProtection.DataStructures.Text;

public interface IMask<TSymbol>
    where TSymbol : struct
{
    IReadOnlyList<TSymbol?> Slots { get; }

    int AcceptChange(ContentChange<TSymbol?> contentChange);
}