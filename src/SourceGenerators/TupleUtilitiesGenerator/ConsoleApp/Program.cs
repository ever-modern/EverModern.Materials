using DestallMaterials.Extensions.Tuples;

var a = (1, 2, 3, 4, 5, 6);

var b = a.Count(i => i % 2 is 0);

Console.WriteLine(b);

return 0;