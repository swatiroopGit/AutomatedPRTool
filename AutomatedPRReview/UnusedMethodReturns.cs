using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AnalyseCodePOC;
using AutomatedPRReview;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.FindSymbols;

class UnusedMethodReturns
{
    static bool CheckForReturn(string code, int lineNumber, string methodName)
    {
        string[] lines = code.Split('\n');
        bool isfirstItr = true;

        while (lineNumber > 0)
        {
            string line = lines[lineNumber - 1].Trim();

            // Check if ";" is present in the line
            if (line.Contains(";") && !isfirstItr && line.IndexOf("return") < line.IndexOf(";"))
            {
                return false;
            }

            // Check if "return" is present in the line
            if (line.Contains("return"))
            {
                return true;
            }

           

            lineNumber--;
            isfirstItr = false;
        }

        return false; // No "return" found in the method
    }





    static void FindMethodCallsAndAsserts(string className, string sourceCode, List<string> methodNamesToFind)
    {

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        SyntaxNode root = syntaxTree.GetRoot();
        string methodName = "";

        var methodCallsAndAsserts = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation =>
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    if (memberAccess.Name is IdentifierNameSyntax identifier &&
                        methodNamesToFind.Contains(identifier.Identifier.ValueText))
                    {
                        return true;
                    }
                }
                else if (invocation.Expression is IdentifierNameSyntax identifier &&
                         methodNamesToFind.Contains(identifier.Identifier.ValueText))
                {
                    return true;
                }

                return false;
            })
            .Select(invocation =>
            {
                methodName = (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                    ? ((IdentifierNameSyntax)memberAccess.Name).Identifier.ValueText
                    : ((IdentifierNameSyntax)invocation.Expression).Identifier.ValueText;

                var containingLine = invocation.GetLocation().GetLineSpan().StartLinePosition.Line;
                var containingStatement = invocation.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
                var containsAssert = containingStatement != null && SyntaxContainsAssert(containingStatement);

                return new
                {
                    CallLine = containingLine + 1, // Adjusted to display line number instead of zero-based index
                    LineContent = sourceCode.Split('\n')[containingLine].Trim(), // Get the content of the line
                    ContainsAssert = containsAssert
                };
            });

        if (methodCallsAndAsserts != null && methodCallsAndAsserts.Any())
        {
            foreach (var methodCallAndAssert in methodCallsAndAsserts)
            {
                bool isReturnStmt = false;
                if(methodCallAndAssert.LineContent.Contains("return"))
                {
                    isReturnStmt = true;
                }
                else
                {
                    isReturnStmt = CheckForReturn(sourceCode, methodCallAndAssert.CallLine, methodName);
                }
                    
                if (methodCallAndAssert.ContainsAssert)
                {
                    // Handle cases where the call contains an assert if needed
                }
                else
                {
                    string _variable = ExtractVariableFromCodeLine(methodCallAndAssert.LineContent);
                    if (_variable.Equals("") && !isReturnStmt)
                    {
                        CodeReviewTool.AssertIssues.Add($"{className} line {methodCallAndAssert.CallLine}");
                    }
                }
            }
        }
    }

   

    static bool SyntaxContainsAssert(SyntaxNode syntaxNode)
    {
        return syntaxNode.DescendantTokens().Any(token => token.IsKind(SyntaxKind.IdentifierToken) && token.Text == "ClassicAssert");
    }

    internal static void FindLineNumberWhereCallIsDone(Dictionary<string, string> AllClasses, List<string> methodNamesToFind)
    {
        foreach (var pair in AllClasses)
        {
            FindMethodCallsAndAsserts(pair.Key, File.ReadAllText(pair.Value), methodNamesToFind);
        }
    }

    internal static void FindunusedReturns(List<ClassPathContainer> AllClasses, List<string> methodNamesToFind)
    {
        foreach (var obj in AllClasses)
        {
            Unusedreturns.FindTheUnusedReturns(obj.Classname, File.ReadAllText(obj.Path), methodNamesToFind);
        }
    }

    static string ExtractVariableFromCodeLine(string codeLine)
    {
        // Define a regular expression pattern to match variable declarations
        string pattern = @"\b(\w+)\s*=\s*.+";


        Match match = Regex.Match(codeLine, pattern);

        if (match.Success && match.Groups.Count > 1)
        {
            // The variable name is in the first capturing group
            return match.Groups[1].Value;
        }

        // Return an empty string if no match or capturing group found
        return string.Empty;
    }

    
}
