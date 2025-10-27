using DestallMaterials.Blazor.Components.Common;
using DestallMaterials.Blazor.Components.Services.UI;
using Microsoft.AspNetCore.Components;

namespace DestallMaterials.Blazor.Components.Inputs;

public partial class MaskedInput
{
    public MaskedInput()
    {
        _inputId = $"masked-input-{this.GetHashCode()}";
        OnValueChanged = _ => { };
        AllowedSymbols = _ => [];
    }

    [Parameter]
    [EditorRequired]
    public string Mask { get; set; } = "";

    [Parameter]
    [EditorRequired]
    public Action<char?[]> OnValueChanged { get; set; }

    [Parameter]
    [EditorRequired]
    public Func<char?[], char[][]> AllowedSymbols { get; set; }

    [Parameter]
    public char SymbolSign { get; set; } = '*';

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        _symbols = Mask.Where((c) => c == SymbolSign)
            .Select(
                (c, i) =>
                {
                    if (_symbols.Length > i)
                    {
                        return _symbols[i];
                    }
                    return null;
                }
            )
            .ToArray();

        _displayText = FormatMask();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            BindToLifetime(
                globalClickCatcher.OnKeyPressed(
                    async (_, _) =>
                    {
                        _lastPosition = await GetCarretPositionAsync();
                    }
                )
            );
        }
    }

    readonly string _inputId;

    string _displayText = "";

    char?[] _symbols = [];
    int _lastPosition;

    string FormatMask()
    {
        int i = 0;
        string result = new(
            Mask.Select(c =>
                {
                    if (c != SymbolSign)
                    {
                        return c;
                    }

                    var typedSymbol = _symbols.Length > i ? _symbols[i] ?? c : c;
                    i++;
                    return typedSymbol;
                })
                .ToArray()
        );
        return result;
    }

    async Task OnInput(string newValue)
    {
        var position = await GetCarretPositionAsync();

        var prevLength = _displayText.Length;

        var lengthIncrease = newValue.Length - prevLength;

        var (differenceStarts, differenceEnds) = StringUtils.FindStringDifference(
            _displayText,
            newValue
        );

        if (differenceStarts == -1)
        {
            ReplaceSymbols(0, newValue.Length, newValue);
        }
        else if (differenceEnds == -1)
        {
            var charsCount = newValue.Length - differenceStarts;
            ReplaceSymbols(
                differenceStarts,
                charsCount,
                newValue.AsSpan().Slice(differenceStarts, charsCount)
            );
        }
        else
        {
            var charsCount = newValue.Length - (differenceEnds - differenceStarts);
            ReplaceSymbols(
                differenceStarts,
                charsCount,
                newValue.AsSpan()[differenceStarts..differenceEnds]
            );
        }

        if (lengthIncrease >= 0)
        {
            var insertedAt = position - lengthIncrease;
            var insertedSymbols = newValue.AsSpan()[insertedAt..position];

            var finalPosition = InsertSymbols(insertedAt, insertedSymbols);

            if (finalPosition != -1)
            {
                await MoveCarretAsync(finalPosition);
            }
            else
            {
                await Js.BlurAsync(_inputId);
            }
        }
        else
        {
            var deletedSymbol = newValue[position];

            _symbols[position] = null;

            position--;
            while (position >= 0 && Mask[position] != SymbolSign)
            {
                position--;
            }

            await MoveCarretAsync(position);

            await SelectCharsAsync(position, position + 1);
        }

        _displayText = FormatMask();
        _lastPosition = position;
    }

    int ReplaceSymbols(int at, int count, ReadOnlySpan<char> symbols)
    {
        for (int i = 0; i < count; i++) { }
    }

    int GetDifference(string initial, string target)
    {
        for (int i = 0; i < initial.Length; i++)
        {
            var c1 = initial[i];
            if (target.Length <= i || target[i] != c1)
            {
                return i;
            }
        }

        return -1;
    }

    async Task OnClick()
    {
        var carretPosition = await GetCarretPositionAsync();
        _lastPosition = carretPosition;
        if (carretPosition < Mask.Length)
        {
            await SelectCharsAsync(carretPosition, carretPosition + 1);
        }
    }

    IInputManipulator Js => ui.Inputs;

    async Task<int> GetCarretPositionAsync()
    {
        var result = (int)await Js.GetCarretPositionAsync(_inputId);
        GlobalLogger.Debug($"Carret position is {result}");
        return result;
    }

    async Task MoveCarretAsync(int newPosition) =>
        await Js.SetCaretPositionAsync(_inputId, (uint)newPosition);

    async Task SelectCharsAsync(int start, int end) =>
        await Js.SetSelectionRangeAsync(_inputId, (uint)start, (uint)end);
}

/// <summary>
/// Converts 
/// </summary>
public class Mask
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="mask">Value in format of </param>
    /// <param name="calculateAllowedSymbols"></param>
    public Mask(
        IEnumerable<MaskBrick> structure,
        Func<char?[], char[][]> calculateAllowedSymbols
    ) 
    {
        /*Fill the properties.*/
    }


    public static IReadOnlyList<MaskBrick> ParseTemplate(string template)
    {

    }
}

