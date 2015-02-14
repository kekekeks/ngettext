using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.PatternMatching;
using NRefactoryCUBuilder;
using File = NRefactoryCUBuilder.File;

namespace Localizer
{
    internal static class WinFormsDesignerRewriter
    {

        static string GetFullyQualifiedTypeName(EntityDeclaration type)
        {
            var rv = type.Name;
            AstNode node = type.Parent;
            while (node != null)
            {
                if (node is SyntaxTree)
                    break;
                var ns = node as NamespaceDeclaration;
                var td = node as TypeDeclaration;
                if (ns != null)
                {
                    rv = ns.FullName + "." + rv;
                    break;
                }
                if (td != null)
                {
                    rv = td.Name + "+" + rv;
                }
                node = node.Parent;
            }
            return rv;
        }

        class TextPropertyVisitor : DepthFirstAstVisitor
        {
            public List<AssignmentExpression> Found = new List<AssignmentExpression>();
            public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
            {
                var member = assignmentExpression.Left as MemberReferenceExpression;
                var operand = assignmentExpression.Right as PrimitiveExpression;
                if (member != null && operand != null && member.MemberName == "Text" && operand.Value is string)
                    Found.Add(assignmentExpression);
                
                base.VisitAssignmentExpression(assignmentExpression);
            }
        }

        class InitializeComponentCallFinder : DepthFirstAstVisitor
        {
            public List<InvocationExpression> Found = new List<InvocationExpression>();
            public override void VisitInvocationExpression(InvocationExpression invocationExpression)
            {
                var member = invocationExpression.Target as IdentifierExpression;
                if (member != null && member.Identifier == "InitializeComponent")
                    Found.Add(invocationExpression);

                base.VisitInvocationExpression(invocationExpression);
            }
        }

        public static void Rewrite(Solution sol)
        {
            foreach (var project in sol.Projects)
            {
                foreach (var file in project.Files)
                    file.SyntaxTree.Freeze();
                var types = project.Files.Where(f => f.Name.EndsWith(".cs"))
                    .SelectMany(
                        f =>
                            f.SyntaxTree.GetTypes().OfType<TypeDeclaration>()
                                .Select(t => new {File = f, Type = t, TypeName = GetFullyQualifiedTypeName(t)}))
                    .GroupBy(t => t.TypeName)
                    .ToDictionary(g => g.Key, g => g.ToList());
                foreach (var type in types)
                {
                    var designer = type.Value.FirstOrDefault(t => t.File.Name.EndsWith(".Designer.cs"));
                    if (designer == null) continue;
                    var mainType = type.Value.FirstOrDefault(t => !t.File.Name.EndsWith(".Designer.cs"));
                    if (mainType == null) continue;


                    var initializeComponent =
                        designer.Type.Members.OfType<MethodDeclaration>().FirstOrDefault(m => m.Name == "InitializeComponent");
                    if (initializeComponent == null)
                        continue;

                    var visitor = new TextPropertyVisitor();
                    initializeComponent.Body.AcceptVisitor(visitor);
                    if (visitor.Found.Count == 0)
                        return;

                    var mainDoc = new StringBuilderDocument(mainType.File.Content);
                    var designerDoc = new StringBuilderDocument(designer.File.Content);
                    var format = FormattingOptionsFactory.CreateAllman();
                    var options = new TextEditorOptions() {TabsToSpaces = true};

                   

                    using (var mainScript = new DocumentScript(mainDoc, format, options))
                    using (var designerScript = new DocumentScript(designerDoc, format, options))
                    {
                        var method = mainType.Type.Members.OfType<MethodDeclaration>().FirstOrDefault(m => m.Name == "LocalizeComponent");
                        var exprs = new List<AssignmentExpression>();
                        foreach (var expr in visitor.Found)
                        {
                            designerScript.Remove(expr.Parent, true);
                            var newExpr = (AssignmentExpression) expr.Clone();
                            newExpr.Right =
                                new InvocationExpression(
                                    new MemberReferenceExpression(new TypeReferenceExpression(new SimpleType("L")), "_"),
                                    newExpr.Right.Clone());
                            exprs.Add(newExpr);
                        }

                        if (method == null)
                        {
                            var initializeComponentCallers = new InitializeComponentCallFinder();
                            mainType.Type.AcceptVisitor(initializeComponentCallers);

                            method = new MethodDeclaration
                            {
                                Name = "LocalizeComponent",
                                ReturnType = new PrimitiveType("void"),
                                Body = new BlockStatement()
                            };
                            exprs.ForEach(expr => method.Body.Add(expr));
                            mainScript.InsertAfter(mainType.Type.Members.Last(), method);
                            foreach (var call in initializeComponentCallers.Found)
                                mainScript.InsertAfter(call.Parent,
                                    new ExpressionStatement(
                                        new InvocationExpression(new IdentifierExpression("LocalizeComponent"))));
                        }
                        else
                        {
                            foreach (var expr in exprs)
                                mainScript.AddTo(method.Body, new ExpressionStatement(expr));
                        }
                    }

                    System.IO.File.WriteAllText(mainType.File.Name, mainDoc.Text);
                    System.IO.File.WriteAllText(designer.File.Name, designerDoc.Text); 
                }
            }
        }
    }
}