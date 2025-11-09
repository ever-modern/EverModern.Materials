using DestallMaterials.WheelProtection.DataStructures.Text;

namespace DestallMaterials.Tests.XUnit;

public class DateTimeContstraintsSourceTests
{
    static readonly DateFormatting _dateFormatting  = new DateFormatting();
    static readonly DateSlotConstraintsSource _source = new DateSlotConstraintsSource(
        new WheelProtection.DataStructures.Time.DateTimeRange(new(2000, 1, 1), new(2020, 1, 1)),
        _dateFormatting
    );

    [Fact]
    public void AccurateSlots() 
    {
        var date = new DateTime(2010, 1, 1);

        var inputValue = date.ToString(_dateFormatting.ToString()).ToCharArray();

        var constraints = _source.GetConstraints(inputValue);
    }
}
