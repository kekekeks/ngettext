using System;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using NRefactoryCUBuilder;

namespace Localizer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var sol = new Solution(args[0]);
            sol.CreateCompilationUnitsForAllPojects();
            var visitor = new LocalizableStringVisitor();
            foreach (var proj in sol.Projects)
            {
                foreach (var file in proj.Files)
                {
                    if (file.Name.EndsWith("cs"))
                    {
                        foreach (var type in file.SyntaxTree.GetTypes().OfType<TypeDeclaration>())
                            visitor.VisitTypeDeclaration(type);
                    }
                }
            }
            var t = new SecondLanguage.GettextPOTranslation();
            foreach (var s in visitor.Found)
            {
                t.SetString(s, s);
            }
            var pot = args.Length > 1 ? args[1] : sol.Name + ".pot";
            t.SetHeader("Project-Id-Version", sol.Name);
            t.Save(pot);
            Console.WriteLine("Written to " + pot);

        }
    }
}
