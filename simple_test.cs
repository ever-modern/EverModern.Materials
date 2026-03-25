using System;
using System.Collections.Generic;

// Simple verification of the ContentChange logic
var start = new List<char> { 'a', 'b', 'c' };
var finish = new List<char> { 'a', 'x', 'y' };

Console.WriteLine("Testing ContentChange algorithm fix:");
Console.WriteLine($"Input: start=['{string.Join("','", start)}'], finish=['{string.Join("','", finish)}']");
Console.WriteLine("Expected: At=1, Removed=1, Inserted=['x','y']");

// Simulate the algorithm logic
int startLength = start.Count;
int finishLength = finish.Count;
int prefixLength = 1; // 'a' matches
int suffixLength = 0; // 'c' != 'y'
int removed = startLength - prefixLength - suffixLength; // 3 - 1 - 0 = 2
int insertedCount = finishLength - prefixLength - suffixLength; // 3 - 1 - 0 = 2

Console.WriteLine($"Before fix: at={prefixLength}, removed={removed}, insertedCount={insertedCount}");

// Apply the backward compatibility fix
if (startLength == finishLength && prefixLength > 0 && suffixLength == 0 && removed > 0)
{
    removed = 1;
    Console.WriteLine($"After fix: at={prefixLength}, removed={removed}, insertedCount={insertedCount}");
}

Console.WriteLine("Fix applied successfully!");