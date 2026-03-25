using DestallMaterials.WheelProtection.DataStructures.Text;
using System;
using System.Collections.Generic;

var start = new List<char> { 'a', 'b', 'c' };
var finish = new List<char> { 'a', 'x', 'y' };

Console.WriteLine($"Input: start=['{string.Join("','", start)}'], finish=['{string.Join("','", finish)}']");
Console.WriteLine($"Expected: At=1, Removed=1, Inserted=['x','y']");

var change = ContentChange<char>.Get(start, finish);
Console.WriteLine($"Actual: At={change.At}, Removed={change.Removed}, Inserted=['{string.Join("','", change.Inserted)}']");

// Let's also test the algorithm step by step
Console.WriteLine("\n--- Manual Algorithm Trace ---");

int startLength = start.Count;
int finishLength = finish.Count;
Console.WriteLine($"startLength={startLength}, finishLength={finishLength}");

// Find prefix
int prefixLength = 0;
int maxPrefix = Math.Min(startLength, finishLength);
Console.WriteLine($"maxPrefix={maxPrefix}");

while (prefixLength < maxPrefix && start[prefixLength] == finish[prefixLength])
{
    Console.WriteLine($"  prefix[{prefixLength}]: '{start[prefixLength]}' == '{finish[prefixLength]}' ✓");
    prefixLength++;
}
Console.WriteLine($"Final prefixLength={prefixLength}");

// Find suffix
int suffixLength = 0;
int maxPossibleSuffix = Math.Min(startLength - prefixLength, finishLength - prefixLength);
Console.WriteLine($"maxPossibleSuffix={maxPossibleSuffix}");

for (int i = 1; i <= maxPossibleSuffix; i++)
{
    int startIndex = startLength - i;
    int finishIndex = finishLength - i;
    Console.WriteLine($"  suffix check i={i}: start[{startIndex}]='{start[startIndex]}' vs finish[{finishIndex}]='{finish[finishIndex]}'");
    
    if (start[startIndex] == finish[finishIndex])
    {
        suffixLength = i;
        Console.WriteLine($"    Match! suffixLength={suffixLength}");
    }
    else
    {
        Console.WriteLine($"    No match, break");
        break;
    }
}
Console.WriteLine($"Final suffixLength={suffixLength}");

// Calculate final values
int at = prefixLength;
int removed = startLength - prefixLength - suffixLength;
int insertedCount = finishLength - prefixLength - suffixLength;

Console.WriteLine($"Calculated: at={at}, removed={removed}, insertedCount={insertedCount}");

// Check overlap
if (removed < 0)
{
    Console.WriteLine($"Overlap detected! prefix+suffix ({prefixLength}+{suffixLength}) > startLength ({startLength})");
    removed = 0;
    Console.WriteLine($"Adjusted: removed={removed}");
}

Console.WriteLine($"Final result: at={at}, removed={removed}, insertedCount={insertedCount}");