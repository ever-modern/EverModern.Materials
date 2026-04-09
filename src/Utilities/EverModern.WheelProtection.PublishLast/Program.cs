using EverModern.Threading.PublishLast;

string dir = Environment.CurrentDirectory;

#if DEBUG
dir = "C:\\Users\\evermodern\\source\\repos\\ever-modern\\EverModern.Materials\\src";
#endif

var packageFolders = FindPackageFolders(dir);

foreach (var packageFolder in packageFolders)
{
    ActOnReleaseFolder(packageFolder);
}

Console.ReadKey();

static IEnumerable<string> FindPackageFolders(string baseFolder)
{
    var result = Directory.GetFiles(baseFolder, "*.nupkg", SearchOption.AllDirectories)
        .GroupBy(nupkg => Directory.GetParent(nupkg).FullName)
        .Where(gr => gr.Key.EndsWith("Release"))
        .Select(gr => gr.Key)
        .ToArray();

    return result;
}

static void ActOnReleaseFolder(string directory)
{
    const string apiKeyVariableName = "NugetPublishingApiKey";

    string? nugetApiKey = Environment.GetEnvironmentVariable(apiKeyVariableName);

    if (string.IsNullOrEmpty(nugetApiKey))
    {
        Console.WriteLine($"Api key for nuget should be set in environment variable {apiKeyVariableName}.");
        Console.WriteLine($"Aborted");
        return;
    }

    var packages = Directory.GetFiles(directory, "*.nupkg");

    if (packages.Length == 0)
    {
        Console.WriteLine($"No *.nupkg found.");
        return;
    }

    var packagesFromNewestToOldest = packages.OrderByDescending(p => new FileInfo(p).CreationTimeUtc);

    var packageToPush = packagesFromNewestToOldest.First();

    string command = $"dotnet nuget push {packageToPush} --api-key {nugetApiKey} --source https://api.nuget.org/v3/index.json --skip-duplicate";

    Console.WriteLine(@$"Running command:
{command}");

    CmdRunner.Run(command);
}
