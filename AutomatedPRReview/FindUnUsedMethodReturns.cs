using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.FindSymbols;
using System.Reflection;
using System.IO;
using AutomatedPRReview;

namespace AnalyseCodePOC
{
    internal class FindUnUsedMethodReturns : CSharpSyntaxWalker
    {
        private SemanticModel semanticModel;
        private List<string> MethodsWhoReturn;
        private MethodDeclarationSyntax currentDeclarationMethodNode;
        private string currentClassName;
        private string methodBody;

        internal FindUnUsedMethodReturns(SemanticModel semanticModel, List<string> MethodsWhoReturn)
        {
            this.semanticModel = semanticModel;
            this.MethodsWhoReturn = MethodsWhoReturn;
        }
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            string methodName = GetMethodName(node);
            if (MethodsWhoReturn.Contains(methodName))
            {
                CheckNodeForAncestor(node,methodName);
            }


                base.VisitInvocationExpression(node);
        }

        internal void CheckNodeForAncestor(InvocationExpressionSyntax invocationNode,string methodName)
        {
            var parent = invocationNode.Ancestors().FirstOrDefault();
            if (parent is ExpressionStatementSyntax) 
            {
                var grandParent = parent.Ancestors().FirstOrDefault();
                if (grandParent is BlockSyntax)
                {
                    var decendantOfDeclaration = currentDeclarationMethodNode.DescendantNodes().FirstOrDefault(d => d is BlockSyntax);
                    if (grandParent == decendantOfDeclaration)
                    {
                        Console.WriteLine($"Class {currentClassName} at line {invocationNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1} : Unused method found {methodName}");
                        File.AppendAllText($"{CodeReviewTool.ProjectPath}\\CodeAnalysisTool\\Result.txt", $"\nClass {currentClassName} at line {invocationNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1} : Unused method found {methodName}");
                        if(CodeReviewTool.MethodsWhoReturnVoid.Contains(methodName))
                        {
                            Console.WriteLine($"\nAlthough there is another method with the same name which returns void. Ignore this if this refers to that method.\n");
                            File.AppendAllText($"{CodeReviewTool.ProjectPath}\\CodeAnalysisTool\\Result.txt", $"\nAlthough there is another method with the same name which returns void. Ignore this if this refers to that method.\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Class {currentClassName} at line {invocationNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1} : Unused method found {methodName}");
                        File.AppendAllText($"{CodeReviewTool.ProjectPath}\\CodeAnalysisTool\\Result.txt", $"\nClass {currentClassName} at line {invocationNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1} : Unused method found {methodName}");
                        if (CodeReviewTool.MethodsWhoReturnVoid.Contains(methodName))
                        {
                            Console.WriteLine($"\nAlthough there is another method with the same name which returns void. Ignore this if this refers to that method.\n");
                            File.AppendAllText($"{CodeReviewTool.ProjectPath}\\CodeAnalysisTool\\Result.txt", $"\nAlthough there is another method with the same name which returns void. Ignore this if this refers to that method.\n");
                        }
                    }
                }
            }



           
        }
        internal void Analyze(string className, List<string> MethodsWhoReturn)
        {
            currentClassName = className;
            var methods = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                currentDeclarationMethodNode = method;
                this.methodBody = method.Body.ToString();
                Visit(method);
            }
        }


        internal string GetMethodName(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name is IdentifierNameSyntax identifier)
                {
                    return memberAccess.Name.ToString();
                }
            }
            else if (node.Expression is IdentifierNameSyntax identifier)
            {
                return identifier.Identifier.ValueText;
            }
            return "Method name not found";
        }


    }

    internal static class Unusedreturns
    {
        public static void FindTheUnusedReturns(string className, string code, List<string> MethodsWhoReturn)
        {

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
                .AddSyntaxTrees(tree);

            var semanticModel = compilation.GetSemanticModel(tree);
            var findUnUsedMethodReturns = new FindUnUsedMethodReturns(semanticModel, MethodsWhoReturn);
          //  findUnUsedMethodReturns.Visit(tree.GetRoot());
            findUnUsedMethodReturns.Analyze(className,MethodsWhoReturn);
        }
    }
}
