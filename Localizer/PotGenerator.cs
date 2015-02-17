using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using NRefactoryCUBuilder;

namespace Localizer
{
    class PotGenerator
    {
        public static void Generate(Solution sol, string output)
        {
            var hset = new HashSet<string>();
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
                if (hset.Add(s))
                    t.SetString(s, s);
            }
            t.SetHeader("Project-Id-Version", sol.Name);
            

            t.Save(output);
        }
    }
}
