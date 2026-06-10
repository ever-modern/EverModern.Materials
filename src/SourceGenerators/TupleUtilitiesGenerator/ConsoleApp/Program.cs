using EverModern.Extensions.Tuples;

var a = (1, 2, 3, 4, 5, 6);

const int zero = 0;

var b = a.Count(i => i % 2 is zero);

Console.WriteLine(b);

return 0;