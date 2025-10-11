using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.Linq.Enumerable;


namespace SourceGenerator
{
    public static class TupleCodeGeneration
    {
        public const string ExtensionClassName = "TupleExtensions";
        public const string ExtensionNamespace = "DestallMaterials.Extensions.Tuples";

        public static StringBuilder GenerateExtensionClass(this IEnumerable<TupleExpressionSyntax> tupleSyntaxes, Compilation compilation)
        {
            
            var tuples = tupleSyntaxes
                .GroupBy(s => s.SyntaxTree)
                .SelectMany(s =>
                {
                    var semanticModel = compilation.GetSemanticModel(s.Key);
                    return s.Select(ss => semanticModel.GetDeclaredSymbol(ss));
                })
                .Distinct(SymbolEqualityComparer.Default)
                .OfType<INamedTypeSymbol>()
                .ToArray();

            var extensionMethods = new
            {
                TasksRelated = tuples
                    .SelectMany(t => t.ComposeTasksTupleExtensionsSyntax())
                    .ToArray(),
                General = tuples
                    .DistinctBy(t => t.TupleElements.Length)
                    .SelectMany(t => TupleCodeGeneration.ComposeLinqExtensions(t.TupleElements.Length))
                    .ToArray()
            };


            var result = new StringBuilder($"namespace {ExtensionNamespace}\n{{\n\tpublic static class {ExtensionClassName}\n{{\n\t\t");

            foreach (var method in extensionMethods.TasksRelated.Concat(extensionMethods.General))
            {
                result.AppendLine(method);
            }

            result.AppendLine("\t}\n}");

            return result;
        }

        public static IEnumerable<string> MakeTupleExtensionMethods(this INamedTypeSymbol tuple)
        {
            if (tuple.TupleElements.Length == 0)
            {
                yield break;
            }

            var linq = ComposeLinqExtensions(tuple.TupleElements.Length);
            var taskMethod = TaskExtensionMethod(tuple);

            if (taskMethod != null)
            {
                yield return taskMethod;
            }

            foreach (var method in linq)
            {
                yield return method;
            }
        }

        enum TaskVariant
        {
            Task, ValueTask
        }

        static string Of(this TaskVariant taskVariant, string returnType)
        {
            var result = taskVariant == TaskVariant.Task ? taskTypeSignature : valueTaskTypeSignature;

            if (returnType != null)
            {
                result += $"<{returnType}>";
            }

            return result;
        }


        const string taskTypeSignature = "System.Threading.Tasks.Task";
        const string valueTaskTypeSignature = "System.Threading.Tasks.ValueTask";
        static (bool IsTask, TaskVariant Variant, ITypeSymbol ReturnType) IsTask(this ITypeSymbol type)
        {
            var nts = type as INamedTypeSymbol;
            if (nts is null)
            {
                return default;
            }
            var displayString = nts.ToDisplayString();

            var isReferenceTask = displayString.StartsWith(taskTypeSignature);
            var isValueTask = displayString.StartsWith(valueTaskTypeSignature);

            if (isReferenceTask || isValueTask)
            {
                var variant = isReferenceTask ? TaskVariant.Task : TaskVariant.ValueTask;
                var typeArguments = nts.TypeArguments;
                if (typeArguments.Length > 1)
                {
                    return default;
                }

                return (true, variant, typeArguments.FirstOrDefault());
            }

            return default;
        }


        static IEnumerable<string> ComposeSumExtensions(INamedTypeSymbol numbersTuple)
        {
            yield break;
        }

        static IEnumerable<string> ComposeLinqExtensions(int elementsCount)
        {
            var ts = Repeat("T", elementsCount).Merge();
            var touts = Repeat("TOut", elementsCount).Merge();
            yield return $@"public static System.Collections.Generic.IEnumerable<T> AsEnumerable<T>(this ({ts}) items)
            {{
                {Range(1, elementsCount).Select(n => $"yield return items.Item{n};").Merge("\n")}
            }}";
            yield return $@"public static ({touts}) Select<T, TOut>(this ({ts}) items, System.Func<T, TOut> selector)
                    => ({Range(1, elementsCount).Select(i => $"selector(items.Item{i})").Merge()});";
            yield return $@"public static bool Any<T>(this ({ts}) items, System.Func<T, bool> selector)
                    => ({Range(1, elementsCount).Select(i => $"selector(items.Item{i})").Merge(" || ")});";
            yield return $@"public static bool All<T>(this ({ts}) items, System.Func<T, bool> selector)
                    => ({Range(1, elementsCount).Select(i => $"selector(items.Item{i})").Merge(" && ")});";
            yield return $@"public static T First<T>(this ({ts}) items, System.Func<T, bool> selector)
            {{
                {Range(1, elementsCount).Select(i => $"if (selector(items.Item{i})) {{ return items.Item{i}; }}").Merge("\n")}
                throw new System.InvalidOperationException(""No elements in the multitude match the predicate."");
            }}";
            yield return $@"public static T FirstOrDefault<T>(this ({ts}) items, System.Func<T, bool> selector)
            {{
                {Range(1, elementsCount).Select(i => $"if (selector(items.Item{i})) {{ return items.Item{i}; }}").Merge("\n")}
                return default;
            }}";
            yield return $@"public static int Count<T>(this ({ts}) items, System.Func<T, bool> selector)
            {{
                int result = 0;
                {Range(1, elementsCount).Select(i => $"if (selector(items.Item{i})) {{ result++; }}").Merge("\n")}
                return result;
            }}";

            yield return $@"public static System.Collections.Generic.IEnumerator<T> GetEnumerator<T>(({ts}) items)
            {{
                {Range(1, elementsCount).Select(i => $"yield return items.Item{i};").Merge("\n")}
            }}";

            if (elementsCount % 2 == 0)
            {
                var half = elementsCount / 2;
                yield return $@"public static System.Collections.Generic.Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
                    this ({Range(0, elementsCount / 2).Select(_ => $"TKey, TValue").Merge()}) items)
                => new System.Collections.Generic.Dictionary<TKey, TValue>({half})
                {{
                    {Range(1, half).Select(i => $"[items.Item{i * 2 - 1}] = items.Item{i * 2}").Merge(",\n")}
                }};";
            }
        }

        static IEnumerable<string> ComposeTasksTupleExtensionsSyntax(
            this INamedTypeSymbol tupleSymbol)
        {
            yield return TaskExtensionMethod(tupleSymbol);
        }

        static string TaskExtensionMethod(INamedTypeSymbol tasksTuple)
        {
            var taskParams = tasksTuple.TupleElements.Select(te => (te.Type, TaskParams: te.Type.IsTask())).ToArray();
            if (!taskParams.Any(t => t.TaskParams.IsTask))
            {
                return null;
            }
            var tasks = taskParams;

            var tasksWithReturnType = tasks
                    .Where(tt => tt.TaskParams.ReturnType != null)
                    .ToArray();

            var q = 1;
            var returnTs = tasks
                .Select((t) => t.TaskParams.IsTask ? $"T{q++}" : UtilityTypes.Nothing.ToString())
                .Merge();

            var b = 1;
            returnTs = tasks
                .Select(t => t.TaskParams.ReturnType == null ? UtilityTypes.Nothing.ToString() : $"T{b++}")
                .Merge();
            var allTs = tasksWithReturnType.Any() ? $"<{tasksWithReturnType.Select((_, i) => $"T{i + 1}").Merge()}>" : "";
            int j = 1;
            int k = 1;
            var result = $@"public static System.Runtime.CompilerServices.TaskAwaiter<({returnTs})> 
                        GetAwaiter{allTs}(
                        this ({tasks.Select((tt) => tt.TaskParams.Variant.Of(tt.TaskParams.ReturnType == null ? null : $"T{k++}")).Merge(",\n")}) tasks
                    ) => System.Threading.Tasks.Task.WhenAll({tasks.Select((_, i) => $"tasks.Item{i + 1}").Merge()})
    .ContinueWith(_ => ({tasks.Select((tt, i) => tt.TaskParams.ReturnType == null ? $"new {UtilityTypes.Nothing}()" : $"tasks.Item{i + 1}.Result").Merge()}))
    .GetAwaiter();";

            return result;
        }
    }
}

