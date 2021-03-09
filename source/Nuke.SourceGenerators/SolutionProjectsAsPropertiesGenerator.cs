// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Nuke.SourceGenerators
{
    [Generator]
    public class SolutionProjectsAsPropertiesGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            var allTypes = compilation.Assembly.GlobalNamespace.GetAllTypes();
            var membersWithAttributes = allTypes.SelectMany(x => x.GetMembers())
                .Where(x => x is IPropertySymbol || x is IFieldSymbol)
                .Select(x => (Member: x, AttributeData: x.GetAttributeData("global::Nuke.Common.ProjectModel.SolutionAttribute")))
                .Where(x => x.AttributeData != null);

            var rootDirectory = TryGetRootDirectoryFrom(compilation);
            var compilationUnit = CompilationUnit()
                .AddUsings(UsingDirective(IdentifierName("Nuke.Common.ProjectModel")));

            foreach (var (member, attributeData) in membersWithAttributes)
            {
                var attributeArguments = attributeData.NamedArguments;
                if (attributeArguments.SingleOrDefault(x => x.Key == "GenerateProjects").Value.Value as bool? != true)
                    continue;

                var solutionFile = GetSolutionFileFromParametersFile(rootDirectory, member.Name);

                var projectPostfix = attributeArguments.SingleOrDefault(x => x.Key == "ProjectPostfix").Value.Value as string ?? "Project";
                var projectTrimPrefix = attributeArguments.SingleOrDefault(x => x.Key == "ProjectTrimPrefix").Value.Value as string ?? string.Empty;
                var projectSubType = attributeArguments.SingleOrDefault(x => x.Key == "ProjectSubType").Value.Value as string;

                var projects = GetProjectNames(solutionFile)
                    .Select(x => (
                        OriginalName: x,
                        MemberName: x
                            .Replace(".", string.Empty)
                            .Replace("-", string.Empty)
                            .TrimStart(projectTrimPrefix)
                            .Concat(projectPostfix)));

                var containingType = member.ContainingType;
                var classDeclaration = ClassDeclaration(projectSubType ?? containingType.Name)
                    .AddModifiers(Token(SyntaxKind.PartialKeyword))
                    .AddMembers(projects
                        .Select(x => ParseMemberDeclaration($@"public Project {x.MemberName} => {member.Name}.GetProject(""{x.OriginalName}"");"))
                        .ToArray());

                if (projectSubType != null)
                {
                    classDeclaration = ClassDeclaration(containingType.Name)
                        .AddMembers(classDeclaration);
                }

                compilationUnit = compilationUnit
                    .AddMembers(
                        containingType.ContainingNamespace.Equals(compilation.GlobalNamespace, SymbolEqualityComparer.Default)
                            ? NamespaceDeclaration(IdentifierName(containingType.ContainingNamespace.GetFullName()))
                                .AddMembers(classDeclaration)
                            : classDeclaration);
            }

            var source = compilationUnit.NormalizeWhitespace().ToFullString();
            context.AddSource(nameof(SolutionProjectsAsPropertiesGenerator), source);
        }

        private static string GetSolutionFileFromParametersFile(string rootDirectory, string memberName)
        {
            var parametersFile = Path.Combine(rootDirectory, ".nuke", "parameters.json");
            Trace.Assert(File.Exists(parametersFile), $"File.Exists({parametersFile})");
            var jobject = JObject.Parse(File.ReadAllText(parametersFile));
            var memberProperty = jobject[memberName];
            Trace.Assert(memberProperty != null, "memberProperty != null");
            return Path.Combine(rootDirectory, memberProperty.Value<string>());
        }

        private IEnumerable<string> GetProjectNames(string solutionPath)
        {
            static string GuidPattern(string text)
                => $@"\{{(?<{Regex.Escape(text)}>[0-9a-fA-F]{{8}}-[0-9a-fA-F]{{4}}-[0-9a-fA-F]{{4}}-[0-9a-fA-F]{{4}}-[0-9a-fA-F]{{12}})\}}";

            static string TextPattern(string name)
                => $@"""(?<{Regex.Escape(name)}>[^""]*)""";

            var projectRegex = new Regex(
                $@"^Project\(""{GuidPattern("typeId")}""\)\s*=\s*{TextPattern("name")},\s*{TextPattern("path")},\s*""{GuidPattern("projectId")}""$");

            var content = File.ReadAllLines(solutionPath);
            for (var i = 0; i < content.Length; i++)
            {
                var match = projectRegex.Match(content[i]);
                if (!match.Success)
                    continue;

                var name = match.Groups["name"].Value;
                var typeId = match.Groups["typeId"].Value;
                if (typeId != "2150E333-8FDC-42A3-9474-1A3956D46DE8")
                    yield return name;
            }
        }

        internal static string TryGetRootDirectoryFrom(Compilation compilation)
        {
            var syntaxPath = compilation.SyntaxTrees.First().FilePath;
            var startDirectory = Path.GetDirectoryName(File.Exists(syntaxPath) ? syntaxPath : Directory.GetCurrentDirectory());
            Trace.Assert(startDirectory != null, "startDirectory != null");
            var rootDirectory = new DirectoryInfo(startDirectory).FindParentDirectory(x => x.GetDirectories(".nuke").Any());
            Trace.Assert(rootDirectory != null, "rootDirectory != null");
            return rootDirectory.FullName;
        }
    }
}
