namespace RealityProgrammer.CSStandard.Interpreter.Expressions {
    public class GetExpression : BaseExpression {
        public BaseExpression Expression { get; protected set; }
        public IdentifierExpression Variable { get; protected set; }

        public GetExpression(BaseExpression expr, IdentifierExpression name) {
            Expression = expr;
            Variable = name;
        }

        public override string ToString() {
            return "GetExpression(" + Expression + ", " + Variable.Name.Lexeme + ")";
        }

        public override object Evaluate(IInterpreter interpreter) {
            return interpreter.EvaluateGetExpression(this);
        }
    }
}
