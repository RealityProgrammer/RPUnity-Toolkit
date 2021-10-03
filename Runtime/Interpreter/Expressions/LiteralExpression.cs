namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public sealed class LiteralExpression : BaseExpression {
        public object Literal { get; private set; }

        public LiteralExpression(object literal) {
            Literal = literal;
        }

        public override string ToString() {
            if (Literal == null) return "LiteralExpression()";

            return "LiteralExpression(" + Literal + " <" + Literal.GetType().FullName + ">)";
        }

        public override int GetHashCode() {
            return Literal == null ? 0 : Literal.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj == null) return false;
            if (obj is LiteralExpression literalExpr) {
                return Literal.Equals(literalExpr.Literal);
            } else {
                return false;
            }
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateLiteralExpression(this);
        }
    }
}
