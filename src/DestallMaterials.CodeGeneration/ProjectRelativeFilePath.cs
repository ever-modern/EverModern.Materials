using DestallMaterials.WheelProtection.Extensions.Strings;
using Microsoft.CodeAnalysis;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace DestallMaterials.CodeGeneration;

/// <summary>
/// File 
/// </summary>
/// <param name="ProjectName"></param>
/// <param name="Folders"></param>
/// <param name="FileName"></param>
public record struct ProjectRelativeFilePath(string ProjectName, IEnumerable<string> Folders, string FileName)
{
    public ProjectRelativeFilePath(string projectName, string fileName) : this(projectName, Enumerable.Empty<string>(), fileName)
    { }

    public override string ToString() => ToString('/');

    string ToString(char separator) => Folders.Any() ? $"{ProjectName}{separator}{Folders.Join(separator)}{separator}{FileName}" :
                           $"{ProjectName}{separator}{FileName}";


    public static ProjectRelativeFilePath Parse(string relativePathAsString)
    {
        var splitted = relativePathAsString.Split('/');

        if (splitted.Length < 2)
        {
            throw new ArgumentException("Unparsable SourceFilePath string.");
        }

        var projectName = splitted.First();
        var fileName = splitted.Last();
        var folders = splitted[1..^1];

        return new(projectName, folders, fileName);
    }

    [Obsolete("Must not be used without parameters", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ProjectRelativeFilePath() : this(default, default, default) { }

    public string FileExtension
    {
        get
        {
            var splitted = FileName.Split('.');
            if (splitted.Length < 2)
            {
                return "";
            }
            return splitted[^1];
        }
    }
}

/// <summary>
/// Holds data about rendered source code.
/// </summary>
/// <param name="Path"></param>
/// <param name="Content"></param>
/// <param name="Virtual"></param>
public record struct CodeFile(ProjectRelativeFilePath Path, string Content, bool Virtual = false)
{
    static readonly Regex _sourceCodeFilesParser = new Regex(@"<(\s)*File(\s)*()(?<Attributes>.*?)(\s*)>(?<Content>(.|\s)*?)<(\s)*/(\s)*File(\s)*>", RegexOptions.Compiled);
    static readonly Regex _attributesParser = new Regex(@"(\S+)=[""']?((?:.(?![""']?\s+(?:\S+)=|\s*\/?[>""']))+.)[""']?");

    public static IEnumerable<CodeFile> ParseMany(string renderingResult)
    {
        var matches = _sourceCodeFilesParser.Matches(renderingResult);

        foreach (var fileFound in matches.AsEnumerable())
        {
            var attributes = ParseAttributes(fileFound.Groups["Attributes"].Value);

            var path = ProjectRelativeFilePath.Parse(attributes.GetValueOrDefault("Path")
                ?? throw new SourceCodeGenerationException("File element did not contain 'Path' attribute."));

            bool pureVirtual = bool.Parse(attributes.GetValueOrDefault("Virtual") ?? "false");

            string code = fileFound.Groups["Content"].Value;

            code = code
                .Split("``")
                .Select(str => str.Replace("/`", ">").Replace('`', '<'))
                .Join("``");

            CodeFile codeFile = new(path, code, pureVirtual);

            yield return codeFile;
        }
    }

    [Obsolete("Must not be used without parameters", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CodeFile() : this(default, default, default) { }

    static Dictionary<string, string> ParseAttributes(string attributes)
    {
        var matches = _attributesParser.Matches(attributes);
        var result = new Dictionary<string, string>(matches.Count);
        foreach (var attr in matches.AsEnumerable())
        {
            var groups = attr.Groups;
            result[groups[1].Value] = groups[2].Value;
        }
        return result;
    }
};