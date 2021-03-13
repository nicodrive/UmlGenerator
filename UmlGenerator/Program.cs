using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Buildalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UmlGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var tree = CSharpSyntaxTree.ParseText(@"
using System;
class TestClass 
{
    TestClass2 t = new TestClass2();
    void TestMethod ()
    {
        int i;
        t.B();
        A();
    }
    void A()
    {
    }
}
class TestClass2
{
    public void B(){}

}

");

            var Mscorlib = MetadataReference.CreateFromFile(System.Runtime.InteropServices.RuntimeEnvironment
                .GetRuntimeDirectory() + "mscorlib.dll");
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] {tree}, references: new[] {Mscorlib});
            var model = compilation.GetSemanticModel(tree);

            var c = new Collector(model);
            c.Visit(await tree.GetRootAsync());

            /*AnalyzerManager manager = new AnalyzerManager("C:\\good291220\\GoodGame\\GoodGame.sln");
            foreach (var keyValuePair in manager.Projects)
            {
                Console.WriteLine(keyValuePair.Value.ProjectFile.Path);
                
                foreach (var result in keyValuePair.Value.Build())
                {
                }
            }*/
        }
    }

    public class Collector : CSharpSyntaxWalker
    {
        private readonly SemanticModel _model;

        public Collector(SemanticModel model)
        {
            _model = model;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            Console.WriteLine("visit " + node.Identifier.Text);
            base.VisitClassDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            Console.WriteLine("visit method " + node.Identifier.Text);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            
            switch (node.Expression)
            {
                case IdentifierNameSyntax identifierNameSyntax:
                    Console.WriteLine("visit call 1" + identifierNameSyntax.Identifier.Text);
                    break;
                case MemberAccessExpressionSyntax memberAccessExpressionSyntax:
                    var firstChild = memberAccessExpressionSyntax.ChildNodes().ElementAt(0);
                    var typeInfo = _model.GetTypeInfo(firstChild).Type as INamedTypeSymbol;
                    var typeName = typeInfo.Name;
                    var typeNameSpace = typeInfo.ContainingNamespace.Name;
                    
                    Console.WriteLine("visit call 2" + memberAccessExpressionSyntax.Name.Identifier.Text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            base.VisitInvocationExpression(node);
        }
    }

    public record ClassInfo(string Name, ImmutableDictionary<string, MethodInfo> Methods);

    public record MethodInfo(string Name, ImmutableList<EntityInfo> ActionInfos);

    public record EntityInfo();

    public record CallEntityInfo(string Type, string Method) : EntityInfo();

    public record CommentEntityInfo(string Text);
}