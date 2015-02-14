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
            if (args.Length > 1)
            {
                if (args[1] == "--designer.cs")
                {
                    WinFormsDesignerRewriter.Rewrite(sol);
                    return;
                }
            }
            var pot = args.Length > 1 ? args[1] : (sol.Name + ".pot");
            PotGenerator.Generate(sol, pot);
            Console.WriteLine("Written to " + pot);

        }
    }
}
