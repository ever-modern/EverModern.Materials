namespace SourceGeneratorAsPackageTest;

class NewClass
{
    public async Task TestAsync()
    {
        var a = await (Task.Delay(100), Task.FromResult(500));
    }
}
