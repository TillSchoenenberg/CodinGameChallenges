using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace CodinGame.FileMerger
{
    internal class Program
    {
        const string ProjectName = "PodRacer";
        const string InputFolder = $"C:\\Projekte\\CodinGame\\CodinGameChallenges\\{ProjectName}";
        const string OutputFile =  $"C:\\Projekte\\CodinGame\\{ProjectName}.cs";
        static void Main(string[] args)
        {
            var usingSet = new HashSet<string>();
            var codeBlocks = new List<(string Path, string Code)>();

            foreach (var file in Directory.EnumerateFiles(InputFolder, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(text);
                var root = tree.GetCompilationUnitRoot();

                // Top-Level Usings sammeln
                foreach (var u in root.Usings)
                    usingSet.Add(u.ToFullString().Trim());

                // Restlicher Code ohne Top-Level Usings
                var codeWithoutUsings = root
                    .WithUsings(new SyntaxList<UsingDirectiveSyntax>())
                    .NormalizeWhitespace()
                    .ToFullString();

                codeBlocks.Add((file, codeWithoutUsings));
            }

            var sb = new StringBuilder();
            sb.AppendLine("// Auto-generiert mit Roslyn");
            foreach (var u in usingSet.OrderBy(s => s))
                sb.AppendLine(u);

            sb.AppendLine();

            foreach (var (path, code) in codeBlocks
                         .OrderBy(c => Path.GetFileName(c.Path).Equals("Program.cs", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                         .ThenBy(c => c.Path))
            {
                sb.AppendLine($"#region {Path.GetFileName(path)}");
                sb.AppendLine(code);
                sb.AppendLine("#endregion");
            }

            string tmp = sb.ToString();
            int idx = tmp.IndexOf("#region Program.cs", StringComparison.OrdinalIgnoreCase);
            string subString = "using System;" + System.Environment.NewLine + tmp.Substring(idx);

            File.WriteAllText(OutputFile, subString, Encoding.UTF8);
            Console.WriteLine($"Merge erzeugt: {OutputFile}");

        }
    }
}
