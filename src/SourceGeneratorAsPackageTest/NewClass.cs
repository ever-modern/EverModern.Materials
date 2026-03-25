namespace SourceGeneratorAsPackageTest;

class NewClass
{
    public async Task TestAsync()
    {
        await Task.Delay(100);
        var a = await Task.FromResult(500);
    }
}
