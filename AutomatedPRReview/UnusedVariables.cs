using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomatedPRReview;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class UnusedVariableWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel semanticModel;
    private readonly HashSet<ISymbol> referencedSymbols = new HashSet<ISymbol>();
    private readonly HashSet<ISymbol> passedArguments = new HashSet<ISymbol>();

    internal UnusedVariableWalker(SemanticModel semanticModel)
    {
        this.semanticModel = semanticModel;
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var symbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol != null)
        {
            referencedSymbols.Add(symbol);
        }

        base.VisitIdentifierName(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Name is IdentifierNameSyntax identifier )
            {
                var symbol = semanticModel.GetSymbolInfo(node).Symbol;
                if (symbol != null)
                {
                    referencedSymbols.Add(symbol);
                }
            }
        }
        else if (node.Expression is IdentifierNameSyntax identifier)
        {
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (symbol != null)
            {
                referencedSymbols.Add(symbol);
            }
        }






        var methodSymbol = semanticModel.GetSymbolInfo(node.Expression).Symbol as IMethodSymbol;
        if (methodSymbol != null)
        {
            foreach (var argument in node.ArgumentList.Arguments)
            {
                var argSymbol = semanticModel.GetSymbolInfo(argument.Expression).Symbol;
                if (argSymbol != null)
                {
                    passedArguments.Add(argSymbol);
                }
            }
        }

        base.VisitInvocationExpression(node);
    }



    internal void Analyze(string className)
    {
        var methods = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            // Check only local variables within the method
            var variables = method.DescendantNodes().OfType<VariableDeclaratorSyntax>();

            foreach (var variable in variables)
            {
                var symbol = semanticModel.GetDeclaredSymbol(variable);
                int? startLine = null;
                if (symbol != null && !referencedSymbols.Contains(symbol) && !passedArguments.Contains(symbol))
                {
                    var lineSpan = variable.GetLocation().GetLineSpan();
                    startLine = lineSpan.StartLinePosition.Line + 1; // Line numbers start from 1
                    Console.WriteLine($"Class {className}, Method {method.Identifier} line {startLine} : Unused variable found '{symbol.Name}'");
                    File.AppendAllText($"{CodeReviewTool.ProjectPath}\\CodeAnalysisTool\\Result.txt", $"\nClass {className}, Method {method.Identifier} line {startLine} : Unused variable found '{symbol.Name}'");

                    CodeReviewTool.UsedVariables.Add($"{className}, Method {method.Identifier} line {startLine.ToString()}");
                }
            }
        }
    }

    internal void Analyze_ForReturnedMethods(string className)
    {
        var methods = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            // Check only local variables within the method
            var invokes = method.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invoke in invokes)
            {
              //  var symbol = semanticModel.GetDeclaredSymbol(invoke);
                var symbol = semanticModel.GetSymbolInfo(invoke.Expression).Symbol as IMethodSymbol;
                int? startLine = null;

                if (symbol != null && !referencedSymbols.Contains(symbol) && !passedArguments.Contains(symbol))
                {
                    var lineSpan = invoke.GetLocation().GetLineSpan();
                    startLine = lineSpan.StartLinePosition.Line + 1; // Line numbers start from 1
                    Console.WriteLine($"Class {className}, Method {method.Identifier} line {startLine} : Unused invokation found '{symbol.Name}'");

                    CodeReviewTool.UsedVariables.Add($"{className}, Method {method.Identifier} line {startLine.ToString()}");
                }
            }
        }
    }

}

class UnusedVariables
{
    static void Mainjhjj()
    {
        // Replace 'YourCodeFile.cs' with the actual C# file you want to analyze
        FindUnusedVariables("TestClass", "C:\\Users\\swpadhi\\source\\repos\\AnalyseCodePOC\\AnalyseCodePOC\\TestClass.cs");
    }

    public static void FindUnusedVariables(string className, string filePath)
    {
        string code = File.ReadAllText(filePath);

        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("MyCompilation")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
            .AddSyntaxTrees(tree);

        var semanticModel = compilation.GetSemanticModel(tree);
        var unusedVariableWalker = new UnusedVariableWalker(semanticModel);
        unusedVariableWalker.Visit(tree.GetRoot());
        unusedVariableWalker.Analyze(className);

    }
}
