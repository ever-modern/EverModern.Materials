using DestallMaterials.WheelProtection.DataStructures.Text;

namespace DestallMaterials.Tests.XUnit;

public class ConstraintUtilitiesTests
{
    [Fact]
    public void Range_ToCharOptions()
    {
        const int from = 1975;
        const int to = 2025;

        var result = SlotOptionFunctions.GetOptionsForSlot(2, "2075", 4, from, to);

        Assert.Equal(['0', '1', '2'], result);
    }

    [Fact]
    public void Range_ForPush()
    {
        const int from = 1975;
        const int to = 2025;

        var result = SlotOptionFunctions.GetOptionsForSlot(0, "1***", 4, from, to);

        Assert.Equal(['1', '2'], result);
    }
}
