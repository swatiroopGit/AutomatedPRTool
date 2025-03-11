using AutomatedPRReview;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

internal class NamingConventionCheck
{
    internal static void VerifyNamingConvention(string projectPath)
    {
        List<string> violations = CheckNamingConventions(projectPath);

        if (violations.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
          //  Console.WriteLine("Naming convention violations:");
            foreach (var violation in violations)
            {
                Console.WriteLine(violation);
                sb.Append(violation+"\n");

            }
            File.AppendAllText($"{CodeReviewTool.ProjectPath}\\CodeAnalysisTool\\Result.txt", $"\n{sb.ToString()}");

        }
        else
        {
          //  Console.WriteLine("No naming convention violations found!");
        }
    }

    static List<string> CheckNamingConventions(string projectPath)
    {
        List<string> namingViolations = new List<string>();

        // Find all C# files in the project directory
        var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            // Parse the C# code file
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var root = tree.GetRoot();

            // Get the class name from the file
            var className = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text;

            // Check class names
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDeclaration in classDeclarations)
            {
                if (!IsValidPascalCase(classDeclaration.Identifier.Text))
                {
                    var line = tree.GetLineSpan(classDeclaration.Identifier.Span).StartLinePosition.Line + 1;
                    namingViolations.Add($"Incorrect naming convention for class {className}, Line {line}: {classDeclaration.Identifier.Text}");
                }
            }

            // Check method names
            var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var methodDeclaration in methodDeclarations)
            {
                if (!IsValidPascalCase(methodDeclaration.Identifier.Text))
                {
                    var line = tree.GetLineSpan(methodDeclaration.Identifier.Span).StartLinePosition.Line + 1;
                    namingViolations.Add($"Incorrect naming convention for method in class {className}, Line {line}: {methodDeclaration.Identifier.Text}");
                }
            }

            // Check variable names
            var variableDeclarations = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();
            foreach (var variableDeclaration in variableDeclarations)
            {
                if (!IsValidCamelCase(variableDeclaration.Identifier.Text))
                {
                    var line = tree.GetLineSpan(variableDeclaration.Identifier.Span).StartLinePosition.Line + 1;
                    namingViolations.Add($"Incorrect naming convention for variable in class {className}, Line {line}: {variableDeclaration.Identifier.Text}");
                }
            }
        }

        return namingViolations;
    }

    static bool IsValidPascalCase(string name)
    {
        // Example: Check if a name is in PascalCase
        // You can customize this method based on your naming conventions
        return !string.IsNullOrEmpty(name) && char.IsUpper(name[0]) && name.All(char.IsLetterOrDigit);
    }

    static bool IsValidCamelCase(string name)
    {
        // Example: Check if a name is in camelCase
        // You can customize this method based on your naming conventions
        return !string.IsNullOrEmpty(name) && char.IsLower(name[0]) && name.All(char.IsLetterOrDigit);
    }
}
