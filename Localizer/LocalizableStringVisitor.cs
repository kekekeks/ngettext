using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp;

namespace Localizer
{
    class LocalizableStringVisitor : DepthFirstAstVisitor
    {
        public List<string> Found { get; private set; }

        public LocalizableStringVisitor()
        {
            Found=new List<string>();
        }

        class ConcatVisitor : DepthFirstAstVisitor
        {
            public StringBuilder Builder = new StringBuilder();
            public override void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
            {
                    
                Builder.Append(primitiveExpression.Value);
            }
        }

        public override void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            var member = invocationExpression.Target as MemberReferenceExpression;
            if (member != null && member.MemberName == "_" && invocationExpression.Arguments.Count > 0)
            {
                var identifier = member.Target as IdentifierExpression;
                if (identifier != null && identifier.Identifier == "L")
                {
                    var concat = new ConcatVisitor();
                    invocationExpression.Arguments.First().AcceptVisitor(concat);

                    Found.Add(concat.Builder.ToString());
                }
            }
            base.VisitInvocationExpression(invocationExpression);
        }

    }
}