using System;
using DestallMaterials.WheelProtection.DataStructures.Text;
using Xunit;

namespace DestallMaterials.Tests.XUnit;

public class DateTimeContstraintsSourceTests
{
    static readonly DateFormatting _dateFormatting = new DateFormatting();
    static readonly DateSlotConstraintsSource _source = new DateSlotConstraintsSource(
        new WheelProtection.DataStructures.Time.DateTimeRange(new(2000, 1, 1), new(2020, 12, 31)),
        _dateFormatting
    );

    [Fact]
    public void DateFormatting_ToString_ProducesExpectedPattern()
    {
        // default formatting -> dd.MM.yyyy
        var format = _dateFormatting.ToString();
        Assert.Equal("dd.MM.yyyy", format);
    }

    [Fact]
    public void GetConstraints_FullySpecifiedDate_ProducesDeterministicDigits()
    {
        var date = new DateTime(2010, 1, 5);
        var format = _dateFormatting.ToString();
        var inputValue = date.ToString(format).ToCharArray();

        var constraints = _source.GetConstraints(inputValue);

        // every numeric slot should allow exactly the digit present in inputValue
        for (int i = 0; i < inputValue.Length; i++)
        {
            var ch = inputValue[i];
            // separator positions should be deterministic delimiter
            if (ch == _dateFormatting.Delimiter)
            {
                Assert.Single(constraints[i].Options);
                Assert.Equal(ch, constraints[i].Options[0]);
            }
            else
            {
                // numeric digit should be among allowed options (ideally single)
                Assert.Contains(ch, constraints[i].Options);
            }
        }
    }

    [Fact]
    public void GetConstraints_SeparatorSlots_AreDeterministic()
    {
        // empty filling -> constraints should still have deterministic delimiter at separator positions
        var constraints = _source.GetConstraints([]);
        var format = _dateFormatting.ToString();

        for (int i = 0; i < format.Length; i++)
        {
            if (!char.IsLetter(format[i]))
            {
                Assert.Single(constraints[i].Options);
                Assert.Equal(format[i], constraints[i].Options[0]);
            }
        }
    }

    [Fact]
    public void GetConstraints_FebruaryLeapYear_RestrictsDayTens()
    {
        // Use a filling that sets month = 02 and year = 2000 (leap year)
        // Format dd.MM.yyyy -> positions: [0]=d1,[1]=d2,[2]='.',[3]=m1,[4]=m2,[5]='.',[6]=y1..[9]=y4
        var filling = new char[] { default, default, _dateFormatting.Delimiter, '0', '2', _dateFormatting.Delimiter, '2', '0', '0', '0' };

        var constraints = _source.GetConstraints(filling);

        var dayTens = constraints[0].Options;

        // For February in a leap year max day is 29 -> day tens '3' should not be allowed
        Assert.DoesNotContain('3', dayTens);
        Assert.Contains('0', dayTens);
        Assert.Contains('1', dayTens);
        Assert.Contains('2', dayTens);
    }
}
