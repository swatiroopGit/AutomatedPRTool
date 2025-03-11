using AutomatedPRReview;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

internal class CommnetClass
{
    //ssddvfd
    internal static void CheckForComments(string projectPath,List<string> ExcludeFolders)
    {
        List<string> unCommentedMethods = CheckCommentedMethods(projectPath, ExcludeFolders);

        if (unCommentedMethods.Count > 0)
        {
            Console.WriteLine("\nComments missing for below methods:");
            File.AppendAllText($"{CodeReviewTool.ProjectPath}\\CodeAnalysisTool\\Result.txt", "\nComments missing for below methods:");
            StringBuilder sb = new StringBuilder();
            foreach (var method in unCommentedMethods)
            {
                Console.WriteLine(method);
                sb.Append(method+"\n");
                
            }
            File.AppendAllText($"{CodeReviewTool.ProjectPath}\\CodeAnalysisTool\\Result.txt", $"\n{sb.ToString()}");

        }
        else
        {
            
        }
    }

    static List<string> CheckCommentedMethods(string projectPath,List<string> ExcludeFolders)
    {
        List<string> unCommentedMethods = new List<string>();

        // Find all C# files in the project directory
        var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            bool flag = false;
            foreach (var excludepath in ExcludeFolders)
            {
                if (file.StartsWith(excludepath))
                {
                    flag = true;
                }
            }
            if (flag == true) { continue; }
            // Parse the C# code file
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var root = tree.GetRoot();

            // Find all methods in the file
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                // Check if the method has a comment
                if (method.GetLeadingTrivia().All(trivia => !trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                                                         && !trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                                                         && !trivia.ToFullString().StartsWith("/// <summary>")
                                                         && !trivia.ToFullString().StartsWith("/// </summary>")))
                {
                    string className = "";
                    // If the method does not have a comment, add it to the list
                    try
                    {
                         className = method.Ancestors().OfType<ClassDeclarationSyntax>().First().Identifier.Text;
                    }
                    catch(InvalidOperationException)
                    {
                        continue;
                    }
                    var methodName = method.Identifier.Text;
                    if(className.Equals("Program") || className.Equals("Program3") || className.Equals("Program8") || className.Equals("NamingConventionCheck") || className.Equals("CommnetClass"))
                    {
                        continue;
                    }
                    else
                    {
                        unCommentedMethods.Add($"{className}.{methodName}");
                    }
                    
                }
            }
        }

        return unCommentedMethods;
    }
}
