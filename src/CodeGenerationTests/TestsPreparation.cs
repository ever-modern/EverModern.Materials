using DestallMaterials.WheelProtection.Linq;
using Microsoft.CodeAnalysis;
using static System.IO.Directory;

namespace CodeGenerationTests;

static class TestsPreparation
{
    internal static (string supplierProjFile, string consumerProjFile) EnsureSamples()
    {
        var currentDirectory = GetCurrentDirectory();
        var rootTestProjectFolder = GetParent(currentDirectory).Parent.Parent;

        var samplesFolder = $"{rootTestProjectFolder}\\CodegenSamples";

        var targetSamplesFolder = $"{currentDirectory}\\CodegenSamples";

        if (Exists(targetSamplesFolder))
        {
            Delete(targetSamplesFolder, true);
        }

        CopyDirectory(samplesFolder, targetSamplesFolder, true);

        return ("Supplier", "Consumer")
            .Select(projName => $"{targetSamplesFolder}\\CodegenSample.{projName}\\CodegenSample.{projName}.csproj");
    }

    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
