using System.Collections.Generic;

namespace SourceGenerator
{
    public class UtilityType
    {
        public UtilityType(string name, string @namespace, string code)
        {
            Name = name;
            Namespace = @namespace;
            Code = code;
        }

        public string Name { get; }
        public string Namespace { get; }
        public string Code { get; }

        public override string ToString()
            => $"{Namespace}.{Name}";
    }

    public static class UtilityTypes
    {
        public static UtilityType Nothing { get; } = new UtilityType("Nothing", "Destall", $@"namespace Destall
        {{
            public struct Nothing
            {{
            }}
        }}");

        public static IReadOnlyList<UtilityType> All = new UtilityType[]
        {
            Nothing
        };
    }
}

