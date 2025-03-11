using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Reflection;

namespace AutomatedPRReview
{
    public class CodeReviewTool
    {//
        internal static string ProjectPath { get; set; }
        private List<string> excludeFolders;
        internal static List<string> AssertIssues = new List<string>();
        internal static List<string> UsedVariables = new List<string>();
        internal static List<string> MethodsWhoReturnVoid;

       

        public CodeReviewTool(string projectPath,List<string> excludeFolders)
        {
            if(string.IsNullOrEmpty(projectPath) || projectPath == null)
            {
                throw new ArgumentNullException("Project path  must be provided",nameof(projectPath));
            }
            foreach(var folder in excludeFolders)
            {
                if(folder.Equals(""))
                {
                    throw new ArgumentNullException("One for the exclude folders is empty", nameof(projectPath));
                }
            }
            ProjectPath = projectPath;
            this.excludeFolders = excludeFolders;
        }

        public void StartReview()
        {
            excludeFolders.Add(@$"{ProjectPath}\bin");
            excludeFolders.Add(@$"{ProjectPath}\obj");
            var classList = GetClassNamesAndCode(ProjectPath, excludeFolders);
            List<ClassPathContainer> AllClassPaths = GetAllClassPaths(ProjectPath, excludeFolders);
            List<string> AllMethodsWhoReturn = new List<string>();

            foreach (var obj in classList)
            {
                Type type = Type.GetType($"{obj.Namespace}.{obj.Classname}");
                foreach (var ele in GetMethodsReturningValue(type))
                {
                    AllMethodsWhoReturn.Add(ele);
                }
            }
            MethodsWhoReturnVoid = new List<string>();
            foreach (var obj in classList)
            {
                Type type = Type.GetType($"{obj.Namespace}.{obj.Classname}");
                foreach (var ele in GetMethodsReturningVoid(type))
                {
                    MethodsWhoReturnVoid.Add(ele);
                }
            }
            Console.WriteLine($"Scanned {AllClassPaths.Count} classes\n");
            AllMethodsWhoReturn = AllMethodsWhoReturn.Distinct().ToList();
            UnusedMethodReturns.FindunusedReturns(AllClassPaths, AllMethodsWhoReturn);
            Console.WriteLine("");
            File.AppendAllText($"{ProjectPath}\\CodeAnalysisTool\\Result.txt", "\n");


            foreach (var obj in AllClassPaths)
            {
                UnusedVariables.FindUnusedVariables(obj.Classname, obj.Path); // Variables and returned methods
            }


            Console.WriteLine("");
            File.AppendAllText($"{ProjectPath}\\CodeAnalysisTool\\Result.txt", "\n");


            CommnetClass.CheckForComments(ProjectPath, excludeFolders);
            Console.WriteLine("");
            File.AppendAllText($"{ProjectPath}\\CodeAnalysisTool\\Result.txt", "\n");

            NamingConventionCheck.VerifyNamingConvention(ProjectPath);

        }

        List<string> GetMethodsReturningValue(Type type)
        {

            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                     .Where(method => method.ReturnType != typeof(void))
                     .Select(method => method.Name)
                     .ToList();
        }

        List<string> GetMethodsReturningVoid(Type type)
        {

            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                     .Where(method => method.ReturnType == typeof(void))
                     .Select(method => method.Name)
                     .ToList();
        }

        List<ClassPathContainer> GetAllClassPaths(string directoryPath, List<string> ExcludeFolders)
        {
            var classList = new List<ClassPathContainer>();

            try
            {
                foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories))
                {
                    bool flag = false;
                    foreach (var excludepath in ExcludeFolders)
                    {
                        if (filePath.StartsWith(excludepath))
                        {
                            flag = true;
                        }
                    }
                    if (flag == true) { continue; }
                    string code = File.ReadAllText(filePath);

                    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                    var root = tree.GetRoot();

                    var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                    foreach (var classDeclaration in classDeclarations)
                    {
                        ClassPathContainer obj = new ClassPathContainer();
                        obj.Classname = classDeclaration.Identifier.Text;
                        obj.Path = filePath;
                        classList.Add(obj);

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing files: {ex.Message}");
                File.AppendAllText($"{ProjectPath}\\CodeAnalysisTool\\Result.txt", $"\nError processing files: {ex.Message}");

            }

            return classList;
        }
        private List<ClassContainer> GetClassNamesAndCode(string directoryPath, List<string> ExcludeFolders)
        {
            var classList = new List<ClassContainer>();

            try
            {
                foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories))
                {
                    bool flag = false;
                    foreach (var excludepath in ExcludeFolders)
                    {
                        if (filePath.StartsWith(excludepath))
                        {
                            flag = true;
                        }
                    }
                    if (flag == true) { continue; }
                    string code = File.ReadAllText(filePath);

                    SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                    var root = tree.GetRoot();

                    var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                    var namespceDeclarations = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();

                    foreach (var classDeclaration in classDeclarations)
                    {
                        ClassContainer obj = new ClassContainer();
                        obj.Classname = classDeclaration.Identifier.Text;
                        obj.Code = code;
                        try
                        {
                            obj.Namespace = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault().Name.ToString();
                        }
                        catch(NullReferenceException)
                        {
                            File.AppendAllText($"{ProjectPath}\\CodeAnalysisTool\\Result.txt", $"Class does not have a namespace {filePath}");
                        }
                        classList.Add(obj);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing files: {ex.Message}");
                File.AppendAllText($"{ProjectPath}\\CodeAnalysisTool\\Result.txt", $"\nError processing files: {ex.Message}");

            }

            return classList;
        }
    }
}